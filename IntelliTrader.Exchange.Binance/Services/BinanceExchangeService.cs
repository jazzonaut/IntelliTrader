using ExchangeSharp;
using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Exchange.Binance
{
    internal class BinanceExchangeService : ExchangeService
    {
        public const int MAX_TICKERS_AGE_TO_RECONNECT_SECONDS = 60;
        private const int INITIAL_TICKERS_TIMEOUT_SECONDS = 5;
        private const int INITIAL_TICKERS_RETRY_LIMIT = 4;
        private const int SOCKET_DISPOSE_TIMEOUT_SECONDS = 10;

        private ExchangeBinanceAPI binanceApi;
        private IDisposable socket;
        private ConcurrentDictionary<string, Ticker> tickers;
        private BinanceTickersMonitorTimedTask tickersMonitorTimedTask;
        private DateTimeOffset lastTickersUpdate;
        private bool tickersChecked;

        public BinanceExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITasksService tasksService) :
            base(loggingService, healthCheckService, tasksService)
        {

        }

        public override void Start(bool virtualTrading)
        {
            loggingService.Info("Start Binance Exchange service...");

            binanceApi = new ExchangeBinanceAPI();
            binanceApi.RateLimit = new RateGate(Config.RateLimitOccurences, TimeSpan.FromSeconds(Config.RateLimitTimeframe));

            if (!virtualTrading && !String.IsNullOrWhiteSpace(Config.KeysPath))
            {
                if (File.Exists(Config.KeysPath))
                {
                    loggingService.Info("Load keys from encrypted file...");
                    binanceApi.LoadAPIKeys(Config.KeysPath);
                }
                else
                {
                    throw new FileNotFoundException("Keys file not found");
                }
            }

            loggingService.Info("Get initial ticker values...");
            IEnumerable<KeyValuePair<string, ExchangeTicker>> binanceTickers = null;
            for (int retry = 0; retry < INITIAL_TICKERS_RETRY_LIMIT; retry++)
            {
                Task.Run(() => binanceTickers = binanceApi.GetTickers()).Wait(TimeSpan.FromSeconds(INITIAL_TICKERS_TIMEOUT_SECONDS));
                if (binanceTickers != null) break;
            }
            if (binanceTickers != null)
            {
                tickers = new ConcurrentDictionary<string, Ticker>(binanceTickers.Select(t => new KeyValuePair<string, Ticker>(t.Key, new Ticker
                {
                    Pair = t.Key,
                    AskPrice = t.Value.Ask,
                    BidPrice = t.Value.Bid,
                    LastPrice = t.Value.Last
                })));

                lastTickersUpdate = DateTimeOffset.Now;
                healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TickersUpdated, $"Updates: {tickers.Count}");
            }
            else if (tickers != null)
            {
                loggingService.Error("Unable to get initial ticker values");
            }
            else
            {
                throw new Exception("Unable to get initial ticker values");
            }

            ConnectTickersWebsocket();

            loggingService.Info("Binance Exchange service started");
        }

        public override void Stop()
        {
            loggingService.Info("Stop Binance Exchange service...");

            DisconnectTickersWebsocket();
            lastTickersUpdate = DateTimeOffset.MinValue;
            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TickersUpdated);

            loggingService.Info("Binance Exchange service stopped");
        }

        public void ConnectTickersWebsocket()
        {
            try
            {
                loggingService.Info("Connect to Binance Exchange tickers...");
                socket = binanceApi.GetTickersWebSocket(OnTickersUpdated);
                loggingService.Info("Connected to Binance Exchange tickers");

                tickersMonitorTimedTask = tasksService.AddTask(
                    name: nameof(BinanceTickersMonitorTimedTask),
                    task: new BinanceTickersMonitorTimedTask(loggingService, this),
                    interval: MAX_TICKERS_AGE_TO_RECONNECT_SECONDS / 2,
                    startDelay: Constants.TaskDelays.ZeroDelay,
                    startTask: false,
                    runNow: false,
                    skipIteration: 0);
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to connect to Binance Exchange tickers", ex);
            }
        }

        public void DisconnectTickersWebsocket()
        {
            try
            {
                tasksService.RemoveTask(nameof(BinanceTickersMonitorTimedTask), stopTask: true);

                loggingService.Info("Disconnect from Binance Exchange tickers...");
                // Give Dispose 10 seconds to complete and then time out if not
                Task.Run(() => socket.Dispose()).Wait(TimeSpan.FromSeconds(SOCKET_DISPOSE_TIMEOUT_SECONDS));
                socket = null;
                loggingService.Info("Disconnected from Binance Exchange tickers");
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to disconnect from Binance Exchange tickers", ex);
            }
        }

        public override Task<IEnumerable<ITicker>> GetTickers()
        {
            return Task.FromResult(tickers.Values.Select(t => (ITicker)t));
        }

        public override Task<IEnumerable<string>> GetMarketPairs(string market)
        {
            return Task.FromResult(tickers.Keys.Where(t => t.EndsWith(market)));
        }

        public override async Task<Dictionary<string, decimal>> GetAvailableAmounts()
        {
            var results = await binanceApi.GetAmountsAvailableToTradeAsync();
            return results;
        }

        public override async Task<IEnumerable<IOrderDetails>> GetMyTrades(string pair)
        {
            var myTrades = new List<OrderDetails>();
            var results = await binanceApi.GetMyTradesAsync(pair);

            foreach (var result in results)
            {
                myTrades.Add(new OrderDetails
                {
                    Side = result.IsBuy ? OrderSide.Buy : OrderSide.Sell,
                    Result = (OrderResult)(int)result.Result,
                    Date = result.OrderDate,
                    OrderId = result.OrderId,
                    Pair = result.Symbol,
                    Message = result.Message,
                    Amount = result.Amount,
                    AmountFilled = result.AmountFilled,
                    Price = result.Price,
                    AveragePrice = result.AveragePrice,
                    Fees = result.Fees,
                    FeesCurrency = result.FeesCurrency
                });
            }

            return myTrades;
        }

#pragma warning disable CS1998
        public override async Task<decimal> GetAskPrice(string pair)
        {
            if (tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.AskPrice;
            }
            else
            {
                return 0;
            }
        }

#pragma warning disable CS1998
        public override async Task<decimal> GetBidPrice(string pair)
        {
            if (tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.BidPrice;
            }
            else
            {
                return 0;
            }
        }

#pragma warning disable CS1998
        public override async Task<decimal> GetLastPrice(string pair)
        {
            if (tickers.TryGetValue(pair, out Ticker ticker))
            {
                return ticker.LastPrice;
            }
            else
            {
                return 0;
            }
        }

        public override async Task<decimal> GetPriceSpread(string pair)
        {
            if (tickers.TryGetValue(pair, out Ticker ticker))
            {
                return Utils.CalculateMargin(ticker.BidPrice, ticker.AskPrice);
            }
            else
            {
                return 0;
            }
        }

        public override async Task<decimal> GetPriceArbitrage(string pair, string market)
        {
            try
            {
                string mainPair = pair;
                string flippedPair = mainPair.Substring(0, mainPair.Length - market.Length) + (market == Constants.Markets.BTC ? Constants.Markets.ETH : Constants.Markets.BTC);

                if (tickers.TryGetValue(mainPair, out Ticker mainTicker) &&
                    tickers.TryGetValue(flippedPair, out Ticker flippedTicker) &&
                    tickers.TryGetValue(Constants.Markets.ETH + Constants.Markets.BTC, out Ticker marketTicker))
                {
                    if (market == Constants.Markets.BTC)
                    {
                        return 1M / mainTicker.AskPrice * flippedTicker.BidPrice * marketTicker.BidPrice;

                    }
                    else if (market == Constants.Markets.ETH)
                    {
                        return 1M / mainTicker.AskPrice * flippedTicker.BidPrice * marketTicker.AskPrice;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return 1;
                }
            }
            catch
            {
                return 1;
            }
        }

        public override async Task<IOrderDetails> PlaceOrder(IOrder order)
        {
            var result = await binanceApi.PlaceOrderAsync(new ExchangeOrderRequest
            {
                OrderType = (ExchangeSharp.OrderType)(int)order.Type,
                IsBuy = order.Side == OrderSide.Buy,
                Amount = order.Amount,
                Price = order.Price,
                Symbol = order.Pair
            });

            return new OrderDetails
            {
                Side = result.IsBuy ? OrderSide.Buy : OrderSide.Sell,
                Result = (OrderResult)(int)result.Result,
                Date = result.OrderDate,
                OrderId = result.OrderId,
                Pair = result.Symbol,
                Message = result.Message,
                Amount = result.Amount,
                AmountFilled = result.AmountFilled,
                Price = result.Price,
                AveragePrice = result.AveragePrice,
                Fees = result.Fees,
                FeesCurrency = result.FeesCurrency
            };
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
                if (tickers.TryGetValue(update.Key, out Ticker ticker))
                {
                    ticker.AskPrice = update.Value.Ask;
                    ticker.BidPrice = update.Value.Bid;
                    ticker.LastPrice = update.Value.Last;
                }
                else
                {
                    tickers.TryAdd(update.Key, new Ticker
                    {
                        Pair = update.Key,
                        AskPrice = update.Value.Ask,
                        BidPrice = update.Value.Bid,
                        LastPrice = update.Value.Last
                    });
                }
            }
        }
    }
}
