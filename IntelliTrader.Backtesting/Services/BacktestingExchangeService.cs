using ExchangeSharp;
using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Backtesting
{
    public class BacktestingExchangeService : ExchangeService
    {
        private readonly IBacktestingService backtestingService;
        private ConcurrentBag<string> markets;

        public BacktestingExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, 
            ITasksService tasksService, IBacktestingService backtestingService)
            : base(loggingService, healthCheckService, tasksService)
        {
            this.backtestingService = backtestingService;
        }

        public override void Start(bool virtualTrading)
        {
            loggingService.Info("Start Backtesting Exchange service...");

            Api = InitializeApi();

            loggingService.Info("Backtesting Exchange service started");
        }

        public override void Stop()
        {
            loggingService.Info("Stop Backtesting Exchange service...");


            loggingService.Info("Backtesting Exchange service stopped");
        }

        protected override ExchangeAPI InitializeApi()
        {
            return new ExchangeBinanceAPI();
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

        public override IEnumerable<string> GetMarketPairs(string market)
        {
            return backtestingService.GetCurrentTickers().Keys.Where(t => t.EndsWith(market));
        }

        public override decimal GetPrice(string pair, TradePriceType priceType)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                if (priceType == TradePriceType.Ask)
                {
                    return ticker.AskPrice;
                }
                else if (priceType == TradePriceType.Bid)
                {
                    return ticker.BidPrice;
                }
                else
                {
                    return ticker.LastPrice;
                }
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
                    string marketPair = GetArbitrageMarketPair(crossMarket);

                    if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker pairTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(crossMarketPair, out ITicker crossTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(marketPair, out ITicker marketTicker))
                    {
                        if (crossMarket == Constants.Markets.ETH)
                        {
                            // Buy XVGBTC, Sell XVGETH, Sell ETHBTC
                            // decimal directArbitrage = (1 / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice - 1) * 100;
                            // Buy ETHBTC, Buy XVGETH, Sell XVGBTC
                            decimal flipArbitrage = (1 - 1 / pairTicker.BidPrice * crossTicker.AskPrice * marketTicker.AskPrice) * 100;
                            return flipArbitrage;
                        }
                        else if (crossMarket == Constants.Markets.BNB)
                        {
                            // Buy XVGBTC, Sell XVGBNB, Sell BNBBTC
                            // decimal directArbitrage = (1 / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice - 1) * 100;
                            // Buy BNBBTC, Buy XVGBNB, Sell XVGBTC
                            decimal flipArbitrage = (1 - 1 / pairTicker.BidPrice * crossTicker.AskPrice * marketTicker.AskPrice) * 100;
                            return flipArbitrage;
                        }
                        else if (crossMarket == Constants.Markets.USDT)
                        {
                            // Buy XVGBTC, Sell XVGUSDT, Buy BTCUSDT
                            //decimal directArbitrage = (1 / pairTicker.AskPrice * crossTicker.BidPrice / marketTicker.AskPrice - 1) * 100;
                            // Buy BTCUSDT, Buy XVGUSDT, Sell XVGBTC
                            decimal flipArbitrage = (1 - 1 / pairTicker.BidPrice * crossTicker.AskPrice / marketTicker.BidPrice) * 100;
                            return flipArbitrage;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public override string GetArbitrageMarketPair(string crossMarket)
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




        #region Not Needed For Backtesting

        public override IOrderDetails PlaceOrder(IOrder order, string originalPair = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ITicker> GetTickers()
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

        #endregion Not Needed For Backtesting
    }
}
