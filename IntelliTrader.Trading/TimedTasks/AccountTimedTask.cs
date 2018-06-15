using IntelliTrader.Core;

namespace IntelliTrader.Trading
{
    internal class AccountTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;

        public AccountTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
        }

        public override void Run()
        {
            tradingService.Account.Refresh();
        }
    }
}
