using ExchangeSharp;
using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Backtesting
{
    public class BacktestingExchangeService : ExchangeService
    {
        private readonly IBacktestingService backtestingService;
        private ConcurrentBag<string> markets;
        private ExchangeBinanceAPI dymmyApi;

        public BacktestingExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITasksService tasksService, IBacktestingService backtestingService)
            : base(loggingService, healthCheckService, tasksService)
        {
            this.backtestingService = backtestingService;
        }

        public override void Start(bool virtualTrading)
        {
            loggingService.Info("Start Backtesting Exchange service...");

            dymmyApi = new ExchangeBinanceAPI();

            loggingService.Info("Backtesting Exchange service started");
        }

        public override void Stop()
        {
            loggingService.Info("Stop Backtesting Exchange service...");


            loggingService.Info("Backtesting Exchange service stopped");
        }

        public override IEnumerable<string> GetMarkets()
        {
            if (markets == null && backtestingService.GetCurrentTickers() != null)
            {
                this.markets = new ConcurrentBag<string>(backtestingService.GetCurrentTickers().Keys
                    .Select(pair => GetPairMarket(pair)).Distinct().ToList());
            }

            if (markets != null)
            {
                return markets.OrderBy(m => m);
            }
            else
            {
                return new List<string>();
            }
        }

        public override string GetPairMarket(string pair)
        {
            return dymmyApi.ExchangeSymbolToGlobalSymbol(pair).Split('-')[0];
        }

        public override decimal GetAskPrice(string pair)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                return ticker.AskPrice;
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetBidPrice(string pair)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                return ticker.BidPrice;
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetLastPrice(string pair)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                return ticker.LastPrice;
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetPriceSpread(string pair)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                return Utils.CalculatePercentage(ticker.BidPrice, ticker.AskPrice);
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetPriceArbitrage(string pair, string crossMarket, string market)
        {
            try
            {
                if (market == Constants.Markets.BTC)
                {
                    string crossMarketPair = pair.Substring(0, pair.Length - market.Length) + crossMarket;
                    string marketPair = GetArbitrageMarket(crossMarket);

                    if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker pairTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(crossMarketPair, out ITicker crossTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(marketPair, out ITicker marketTicker))
                    {
                        if (crossMarket == Constants.Markets.ETH)
                        {
                            // Buy XVGBTC, Sell XVGETH, Sell ETHBTC
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice);
                            // Buy ETHBTC, Buy XVGETH, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / crossTicker.AskPrice * pairTicker.BidPrice * marketTicker.AskPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                        else if (crossMarket == Constants.Markets.BNB)
                        {
                            // Buy XVGBTC, Sell XVGBNB, Sell BNBBTC
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice);
                            // Buy BNBBTC, Buy XVGBNB, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / crossTicker.AskPrice * pairTicker.BidPrice * marketTicker.AskPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                        else if (crossMarket == Constants.Markets.USDT)
                        {
                            // Buy XVGBTC, Sell XVGUSDT, Buy BTCUSDT
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice / marketTicker.AskPrice);
                            // Buy BTCUSDT, Buy XVGUSDT, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / pairTicker.BidPrice * crossTicker.AskPrice / marketTicker.BidPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public override string GetArbitrageMarket(string crossMarket)
        {
            if (crossMarket == Constants.Markets.ETH || crossMarket == Constants.Markets.BTC)
            {
                return Constants.Markets.ETH + Constants.Markets.BTC;
            }
            else if (crossMarket == Constants.Markets.BNB)
            {
                return Constants.Markets.BNB + Constants.Markets.BTC;
            }
            else if (crossMarket == Constants.Markets.USDT)
            {
                return Constants.Markets.BTC + Constants.Markets.USDT;
            }
            else
            {
                throw new NotSupportedException($"Unsupported arbitrage market: {crossMarket}");
            }
        }

        public override IEnumerable<string> GetMarketPairs(string market)
        {
            return backtestingService.GetCurrentTickers().Keys;
        }




        #region Not Needed For Backtesting

        protected override ExchangeAPI InitializeApi()
        {
            throw new NotImplementedException();
        }

        public override IOrderDetails PlaceOrder(IOrder order, string priceCurrency)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, decimal> GetAvailableAmounts()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IOrderDetails> GetTrades(string pair)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ITicker> GetTickers()
        {
            throw new NotImplementedException();
        }

        #endregion Not Needed For Backtesting
    }
}
