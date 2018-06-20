using ExchangeSharp;
using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Exchange.Base
{
    public abstract class ExchangeService : ConfigrableServiceBase<ExchangeConfig>, IExchangeService
    {
        public override string ServiceName => Constants.ServiceNames.ExchangeService;

        protected readonly ILoggingService loggingService;
        protected readonly IHealthCheckService healthCheckService;
        protected readonly ITasksService tasksService;

        public ExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITasksService tasksService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tasksService = tasksService;
        }

        public abstract void Start(bool virtualTrading);

        public abstract void Stop();

        public abstract Task<IEnumerable<ITicker>> GetTickers(string market);

        public abstract Task<IEnumerable<string>> GetMarketPairs(string market);

        public abstract Task<Dictionary<string, decimal>> GetAvailableAmounts();

        public abstract Task<IEnumerable<IOrderDetails>> GetMyTrades(string pair);

        public abstract Task<decimal> GetAskPrice(string pair);

        public abstract Task<decimal> GetBidPrice(string pair);

        public abstract Task<decimal> GetLastPrice(string pair);

        public abstract Task<decimal> GetPriceSpread(string pair);

        public abstract Task<IOrderDetails> PlaceOrder(IOrder order);
    }
}
