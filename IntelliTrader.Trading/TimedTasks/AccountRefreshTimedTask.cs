using IntelliTrader.Core;

namespace IntelliTrader.Trading
{
    public class AccountRefreshTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;

        public AccountRefreshTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
        }

        protected override void Run()
        {
            tradingService.Account.Refresh();
        }
    }
}
