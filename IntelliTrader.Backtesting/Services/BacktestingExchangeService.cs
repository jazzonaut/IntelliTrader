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
    public class BacktestingExchangeService : ConfigrableServiceBase<ExchangeConfig>, IExchangeService
    {
        public override string ServiceName => Constants.ServiceNames.ExchangeService;

        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly IBacktestingService backtestingService;
        private ConcurrentBag<string> markets;

        public BacktestingExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, IBacktestingService backtestingService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.backtestingService = backtestingService;
        }

        public void Start(bool virtualTrading)
        {
            loggingService.Info("Start Backtesting Exchange service...");


            loggingService.Info("Backtesting Exchange service started");
        }

        public void Stop()
        {
            loggingService.Info("Stop Backtesting Exchange service...");


            loggingService.Info("Backtesting Exchange service stopped");
        }

        public Task<IEnumerable<string>> GetMarkets()
        {
            if (markets == null && backtestingService.GetCurrentTickers() != null)
            {
                this.markets = new ConcurrentBag<string>(backtestingService.GetCurrentTickers().Keys
                    .Select(pair => new ExchangeBinanceAPI().ExchangeSymbolToGlobalSymbol(pair).Split('-')[0]).Distinct().ToList());
            }

            if (markets != null)
            {
                return Task.FromResult(markets.OrderBy(m => m).AsEnumerable());
            }
            else
            {
                return Task.FromResult(new List<string>().AsEnumerable());
            }
        }

#pragma warning disable CS1998
        public async Task<decimal> GetAskPrice(string pair)
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


#pragma warning disable CS1998
        public async Task<decimal> GetBidPrice(string pair)
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

#pragma warning disable CS1998
        public async Task<decimal> GetLastPrice(string pair)
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

        public async Task<decimal> GetPriceSpread(string pair)
        {
            if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker ticker))
            {
                return Utils.CalculateMargin(ticker.BidPrice, ticker.AskPrice);
            }
            else
            {
                return 0;
            }
        }

        public async Task<decimal> GetPriceArbitrage(string pair, string crossMarket, string market)
        {
            try
            {
                if (market == Constants.Markets.BTC || market == Constants.Markets.ETH)
                {

                    string crossMarketPair = pair.Substring(0, pair.Length - market.Length) + crossMarket;
                    string marketPair = null;

                    if (crossMarket == Constants.Markets.ETH || pair == Constants.Markets.BTC)
                    {
                        marketPair = Constants.Markets.ETH + Constants.Markets.BTC;
                    }
                    else if (crossMarket == Constants.Markets.BNB)
                    {
                        marketPair = Constants.Markets.BNB + Constants.Markets.BTC;
                    }
                    else if (crossMarket == Constants.Markets.USDT)
                    {
                        marketPair = Constants.Markets.BTC + Constants.Markets.USDT;
                    }

                    if (backtestingService.GetCurrentTickers().TryGetValue(pair, out ITicker pairTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(crossMarketPair, out ITicker crossTicker) &&
                        backtestingService.GetCurrentTickers().TryGetValue(marketPair, out ITicker marketTicker))
                    {
                        if (market == Constants.Markets.BTC)
                        {
                            if (crossMarket == Constants.Markets.ETH)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice, 1);
                            }
                            else if (crossMarket == Constants.Markets.BNB)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice, 1);
                            }
                            else if (crossMarket == Constants.Markets.USDT)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.AskPrice * crossTicker.BidPrice / marketTicker.AskPrice, 1);
                            }

                        }
                        else if (market == Constants.Markets.ETH)
                        {
                            if (crossMarket == Constants.Markets.BTC)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.AskPrice, 1);
                            }
                            else if (crossMarket == Constants.Markets.BNB)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.AskPrice, 1);
                            }
                            else if (crossMarket == Constants.Markets.USDT)
                            {
                                return Utils.CalculateMargin(1M / pairTicker.BidPrice * crossTicker.AskPrice / marketTicker.BidPrice, 1);
                            }
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public Task<IEnumerable<string>> GetMarketPairs(string market)
        {
            return Task.FromResult(backtestingService.GetCurrentTickers().Keys.AsEnumerable());
        }

        #region Not Needed For Backtesting

        public Task<IOrderDetails> PlaceOrder(IOrder order)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, decimal>> GetAvailableAmounts()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IOrderDetails>> GetMyTrades(string pair)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ITicker>> GetTickers()
        {
            throw new NotImplementedException();
        }

        #endregion Not Needed For Backtesting
    }
}
