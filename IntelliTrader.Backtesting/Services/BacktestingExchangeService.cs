using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
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

        public async Task<decimal> GetPriceArbitrage(string pair, string market)
        {
            try
            {
                string mainPair = pair;
                string flippedPair = mainPair.Substring(0, mainPair.Length - market.Length) + (market == Constants.Markets.BTC ? Constants.Markets.ETH : Constants.Markets.BTC);

                if (backtestingService.GetCurrentTickers().TryGetValue(mainPair, out ITicker mainTicker) &&
                    backtestingService.GetCurrentTickers().TryGetValue(flippedPair, out ITicker flippedTicker) &&
                    backtestingService.GetCurrentTickers().TryGetValue(Constants.Markets.ETH + Constants.Markets.BTC, out ITicker marketTicker))
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

        public Task<IEnumerable<ITicker>> GetTickers(string market)
        {
            throw new NotImplementedException();
        }

        #endregion Not Needed For Backtesting
    }
}
