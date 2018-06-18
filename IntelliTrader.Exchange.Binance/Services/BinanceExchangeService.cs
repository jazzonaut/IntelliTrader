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

        private ExchangeBinanceAPI binanceApi;
        private IDisposable socket;
        private ConcurrentDictionary<string, Ticker> tickers;
        private BinanceTickersMonitorTimedTask tickersMonitorTimedTask;
        private DateTimeOffset lastTickersUpdate;
        private bool tickersChecked;

        public BinanceExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService) :
            base(loggingService, healthCheckService)
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
            tickers = new ConcurrentDictionary<string, Ticker>(binanceApi.GetTickers().Select(t => new KeyValuePair<string, Ticker>(t.Key, new Ticker
            {
                Pair = t.Key,
                AskPrice = t.Value.Ask,
                BidPrice = t.Value.Bid,
                LastPrice = t.Value.Last
            })));
            lastTickersUpdate = DateTimeOffset.Now;
            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TickersUpdated, $"Updates: {tickers.Count}");
            ConnectTickersWebsocket();

            loggingService.Info("Binance Exchange service started");
        }

        public override void Stop()
        {
            loggingService.Info("Stop Binance Exchange service...");

            DisconnectTickersWebsocket();
            lastTickersUpdate = DateTimeOffset.MinValue;
            tickers.Clear();
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

                tickersMonitorTimedTask = new BinanceTickersMonitorTimedTask(loggingService, this);
                tickersMonitorTimedTask.Interval = MAX_TICKERS_AGE_TO_RECONNECT_SECONDS / 2;
                Application.Resolve<ICoreService>().AddTask(nameof(BinanceTickersMonitorTimedTask), tickersMonitorTimedTask);
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
                Application.Resolve<ICoreService>().StopTask(nameof(BinanceTickersMonitorTimedTask));
                Application.Resolve<ICoreService>().RemoveTask(nameof(BinanceTickersMonitorTimedTask));

                loggingService.Info("Disconnect from Binance Exchange tickers...");
                // Give Dispose 10 seconds to complete and then time out if not
                Task.Run(() => socket.Dispose()).Wait(TimeSpan.FromSeconds(10));
                socket = null;
                loggingService.Info("Disconnected from Binance Exchange tickers");
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to disconnect from Binance Exchange tickers", ex);
            }
        }

        public override Task<IEnumerable<ITicker>> GetTickers(string market)
        {
            return Task.FromResult(tickers.Values.Where(t => t.Pair.EndsWith(market)).Select(t => (ITicker)t));
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
