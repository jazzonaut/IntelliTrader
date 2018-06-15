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
