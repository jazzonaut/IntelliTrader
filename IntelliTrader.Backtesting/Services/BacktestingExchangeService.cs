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

        public override decimal GetPriceArbitrage(string pair, string tradingMarket, ArbitrageMarket arbitrageMarket, ArbitrageType? arbitrageType = null)
        {
            try
            {
                if (tradingMarket == Constants.Markets.BTC)
                {
                    string marketPair = ChangeMarket(pair, arbitrageMarket.ToString());
                    string arbitragePair = GetArbitrageMarketPair(arbitrageMarket);

                    if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker pairTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(marketPair, out ITicker marketTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(arbitragePair, out ITicker arbitrageTicker))
                    {
                        decimal directArbitrage = 0;
                        decimal reverseArbitrage = 0;

                        if (arbitrageMarket == ArbitrageMarket.ETH)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                            reverseArbitrage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }
                        else if (arbitrageMarket == ArbitrageMarket.BNB)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                            reverseArbitrage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }
                        else if (arbitrageMarket == ArbitrageMarket.USDT)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice / arbitrageTicker.AskPrice - 1) * 100;
                            reverseArbitrage = (arbitrageTicker.BidPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }

                        if (arbitrageType == ArbitrageType.Direct)
                        {
                            return directArbitrage;
                        }
                        else if (arbitrageType == ArbitrageType.Reverse)
                        {
                            return reverseArbitrage;
                        }
                        else
                        {
                            return Math.Max(directArbitrage, reverseArbitrage);
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public override string GetArbitrageMarketPair(ArbitrageMarket arbitrageMarket)
        {
            if (arbitrageMarket == ArbitrageMarket.ETH)
            {
                return Constants.Markets.ETH + Constants.Markets.BTC;
            }
            else if (arbitrageMarket == ArbitrageMarket.BNB)
            {
                return Constants.Markets.BNB + Constants.Markets.BTC;
            }
            else if (arbitrageMarket == ArbitrageMarket.USDT)
            {
                return Constants.Markets.BTC + Constants.Markets.USDT;
            }
            else
            {
                throw new NotSupportedException($"Unsupported arbitrage market: {arbitrageMarket}");
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
