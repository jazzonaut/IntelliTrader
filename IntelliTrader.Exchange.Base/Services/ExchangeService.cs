using ExchangeSharp;
using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Exchange.Base
{
    public abstract class ExchangeService : ConfigrableServiceBase<ExchangeConfig>, IExchangeService
    {
        public const int MAX_TICKERS_AGE_TO_RECONNECT_SECONDS = 60;
        public const int INITIAL_TICKERS_TIMEOUT_SECONDS = 5;
        public const int INITIAL_TICKERS_RETRY_LIMIT = 4;
        public const int SOCKET_DISPOSE_TIMEOUT_SECONDS = 10;

        public override string ServiceName => Constants.ServiceNames.ExchangeService;

        protected readonly ILoggingService loggingService;
        protected readonly IHealthCheckService healthCheckService;
        protected readonly ITasksService tasksService;

        public ExchangeAPI Api { get; private set; }
        public ConcurrentDictionary<string, Ticker> Tickers { get; private set; }

        private IDisposable socket;
        private ConcurrentBag<string> markets;
        private TickersMonitorTimedTask tickersMonitorTimedTask;
        private DateTimeOffset lastTickersUpdate;
        private bool tickersChecked;

        public ExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITasksService tasksService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tasksService = tasksService;
        }

        protected abstract ExchangeAPI InitializeApi();

        public abstract IOrderDetails PlaceOrder(IOrder order, string priceCurrency = null);

        public abstract IEnumerable<IOrderDetails> GetTrades(string pair);

        public abstract decimal GetPriceArbitrage(string pair, string crossMarket, string market);

        public abstract string GetArbitrageMarket(string crossMarket);

        public virtual void Start(bool virtualTrading)
        {
            loggingService.Info("Start Exchange service...");
            Api = InitializeApi();

            if (!virtualTrading && !String.IsNullOrWhiteSpace(Config.KeysPath))
            {
                if (File.Exists(Config.KeysPath))
                {
                    loggingService.Info("Load keys from encrypted file...");
                    Api.LoadAPIKeys(Config.KeysPath);
                }
                else
                {
                    throw new FileNotFoundException("Keys file not found");
                }
            }

            loggingService.Info("Get initial ticker values...");
            IEnumerable<KeyValuePair<string, ExchangeTicker>> exchangeTickers = null;
            for (int retry = 0; retry < ExchangeService.INITIAL_TICKERS_RETRY_LIMIT; retry++)
            {
                Task.Run(() => exchangeTickers = Api.GetTickers()).Wait(TimeSpan.FromSeconds(ExchangeService.INITIAL_TICKERS_TIMEOUT_SECONDS));
                if (exchangeTickers != null) break;
            }
            if (exchangeTickers != null)
            {
                Tickers = new ConcurrentDictionary<string, Ticker>(exchangeTickers.Select(t => new KeyValuePair<string, Ticker>(t.Key, new Ticker
                {
                    Pair = t.Key,
                    AskPrice = t.Value.Ask,
                    BidPrice = t.Value.Bid,
                    LastPrice = t.Value.Last
                })));
                markets = new ConcurrentBag<string>(Tickers.Keys.Select(pair => GetPairMarket(pair)).Distinct().ToList());

                lastTickersUpdate = DateTimeOffset.Now;
                healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TickersUpdated, $"Updates: {Tickers.Count}");
            }
            else if (Tickers != null)
            {
                loggingService.Error("Unable to get initial ticker values");
            }
            else
            {
                throw new Exception("Unable to get initial ticker values");
            }

            ConnectTickersWebsocket();

            loggingService.Info("Exchange service started");
        }

        public virtual void Stop()
        {
            loggingService.Info("Stop Exchange service...");

            DisconnectTickersWebsocket();
            lastTickersUpdate = DateTimeOffset.MinValue;
            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TickersUpdated);

            loggingService.Info("Exchange service stopped");
        }


        public void ConnectTickersWebsocket()
        {
            try
            {
                loggingService.Info("Connect to Exchange tickers...");
                socket = Api.GetTickersWebSocket(OnTickersUpdated);
                loggingService.Info("Connected to Exchange tickers");

                tickersMonitorTimedTask = tasksService.AddTask(
                    name: nameof(TickersMonitorTimedTask),
                    task: new TickersMonitorTimedTask(loggingService, this),
                    interval: MAX_TICKERS_AGE_TO_RECONNECT_SECONDS / 2,
                    startDelay: Constants.TaskDelays.ZeroDelay,
                    startTask: false,
                    runNow: false,
                    skipIteration: 0);
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to connect to Exchange tickers", ex);
            }
        }

        public void DisconnectTickersWebsocket()
        {
            try
            {
                tasksService.RemoveTask(nameof(TickersMonitorTimedTask), stopTask: true);

                loggingService.Info("Disconnect from Exchange tickers...");
                // Give Dispose 10 seconds to complete and then time out if not
                Task.Run(() => socket.Dispose()).Wait(TimeSpan.FromSeconds(SOCKET_DISPOSE_TIMEOUT_SECONDS));
                socket = null;
                loggingService.Info("Disconnected from Exchange tickers");
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to disconnect from Exchange tickers", ex);
            }
        }

        public virtual IEnumerable<string> GetMarkets()
        {
            return markets.OrderBy(m => m).AsEnumerable();
        }

        public virtual IEnumerable<ITicker> GetTickers()
        {
            return Tickers.Values;
        }

        public virtual IEnumerable<string> GetMarketPairs(string market)
        {
            return Tickers.Keys.Where(t => t.EndsWith(market));
        }

        public virtual string GetPairMarket(string pair)
        {
            return Api.ExchangeSymbolToGlobalSymbol(pair).Split('-')[0];
        }

        public virtual string ChangePairMarket(string pair, string newMarket)
        {
            string currentMarket = GetPairMarket(pair);
            return pair.Substring(0, pair.Length - currentMarket.Length) + newMarket;
        }

        public virtual Dictionary<string, decimal> GetAvailableAmounts()
        {
            return Api.GetAmountsAvailableToTradeAsync().Result;
        }

        public virtual decimal GetAskPrice(string pair)
        {
            if (Tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.AskPrice;
            }
            else
            {
                return 0;
            }
        }

        public virtual decimal GetBidPrice(string pair)
        {
            if (Tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.BidPrice;
            }
            else
            {
                return 0;
            }
        }

        public virtual decimal GetLastPrice(string pair)
        {
            if (Tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.LastPrice;
            }
            else
            {
                return 0;
            }
        }

        public virtual decimal GetPriceSpread(string pair)
        {
            if (Tickers.TryGetValue(pair, out Ticker ticker))
            {
                return Utils.CalculatePercentage(ticker.BidPrice, ticker.AskPrice);
            }
            else
            {
                return 0;
            }
        }

        public TimeSpan GetTimeElapsedSinceLastTickersUpdate()
        {
            return DateTimeOffset.Now - lastTickersUpdate;
        }

        private void OnTickersUpdated(IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>> updatedTickers)
        {
            if (!tickersChecked)
            {
                loggingService.Info("Ticker updates are working, good!");
                tickersChecked = true;
            }

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TickersUpdated, $"Updates: {updatedTickers.Count}");

            lastTickersUpdate = DateTimeOffset.Now;

            foreach (var update in updatedTickers)
            {
                if (Tickers.TryGetValue(update.Key, out Ticker ticker))
                {
                    ticker.AskPrice = update.Value.Ask;
                    ticker.BidPrice = update.Value.Bid;
                    ticker.LastPrice = update.Value.Last;
                }
                else
                {
                    Tickers.TryAdd(update.Key, new Ticker
                    {
                        Pair = update.Key,
                        AskPrice = update.Value.Ask,
                        BidPrice = update.Value.Bid,
                        LastPrice = update.Value.Last
                    });

                    var market = GetPairMarket(update.Key);
                    if (!markets.Contains(market))
                    {
                        markets.Add(market);
                    }
                }
            }
        }
    }
}
