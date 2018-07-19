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
            return markets.AsEnumerable() ?? new List<string>();
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

        public override Arbitrage GetArbitrage(string pair, string tradingMarket, List<ArbitrageMarket> arbitrageMarkets = null, ArbitrageType? arbitrageType = null)
        {
            if (arbitrageMarkets == null || !arbitrageMarkets.Any())
            {
                arbitrageMarkets = new List<ArbitrageMarket> { ArbitrageMarket.ETH, ArbitrageMarket.BNB, ArbitrageMarket.USDT };
            }
            Arbitrage arbitrage = new Arbitrage
            {
                Market = arbitrageMarkets.First(),
                Type = arbitrageType ?? ArbitrageType.Direct
            };

            try
            {
                if (tradingMarket == Constants.Markets.BTC)
                {
                    foreach (var market in arbitrageMarkets)
                    {
                        string marketPair = ChangeMarket(pair, market.ToString());
                        string arbitragePair = GetArbitrageMarketPair(market);

                        if (marketPair != pair && 
                            backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker pairTicker) &&
                            backtestingService.GetCurrentTickers().TryGetValue(marketPair, out ITicker marketTicker) &&
                            backtestingService.GetCurrentTickers().TryGetValue(arbitragePair, out ITicker arbitrageTicker))
                        {
                            decimal directArbitragePercentage = 0;
                            decimal reverseArbitragePercentage = 0;

                            if (market == ArbitrageMarket.ETH)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                                reverseArbitragePercentage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }
                            else if (market == ArbitrageMarket.BNB)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                                reverseArbitragePercentage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }
                            else if (market == ArbitrageMarket.USDT)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice / arbitrageTicker.AskPrice - 1) * 100;
                                reverseArbitragePercentage = (arbitrageTicker.BidPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }

                            if ((directArbitragePercentage > arbitrage.Percentage || !arbitrage.IsAssigned) && (arbitrageType == null || arbitrageType == ArbitrageType.Direct))
                            {
                                arbitrage.IsAssigned = true;
                                arbitrage.Market = market;
                                arbitrage.Type = ArbitrageType.Direct;
                                arbitrage.Percentage = directArbitragePercentage;
                            }

                            if ((reverseArbitragePercentage > arbitrage.Percentage || !arbitrage.IsAssigned) && (arbitrageType == null || arbitrageType == ArbitrageType.Reverse))
                            {
                                arbitrage.IsAssigned = true;
                                arbitrage.Market = market;
                                arbitrage.Type = ArbitrageType.Reverse;
                                arbitrage.Percentage = reverseArbitragePercentage;
                            }
                        }
                    }
                }
            }
            catch { }
            return arbitrage;
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
