using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace IntelliTrader.Core
{
    internal class HealthCheckTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ICoreService coreService;
        private readonly ITradingService tradingService;
        private int healthCheckFailures = 0;

        public HealthCheckTimedTask(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, ICoreService coreService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.coreService = coreService;
            this.tradingService = tradingService;
        }

        public override void Run()
        {
            if (coreService.Config.HealthCheckEnabled)
            {
                bool healthCheckFailed = false;
                loggingService.Info("Health check results:");

                foreach (var healthCheck in healthCheckService.GetHealthChecks().OrderBy(c => c.Name))
                {
                    var elapsedSinceLastUpdate = (DateTimeOffset.Now - healthCheck.LastUpdated).TotalSeconds;
                    bool healthCheckTimeout = coreService.Config.HealthCheckSuspendTradingTimeout > 0 && elapsedSinceLastUpdate > coreService.Config.HealthCheckSuspendTradingTimeout;
                    string indicator = (healthCheck.Failed || healthCheckTimeout) ? "[-]" : "[+]";

                    if (healthCheck.Message != null)
                    {
                        loggingService.Info($" {indicator} ({healthCheck.LastUpdated:HH:mm:ss}) {healthCheck.Name} - {healthCheck.Message}");
                    }
                    else
                    {
                        loggingService.Info($" {indicator} ({healthCheck.LastUpdated:HH:mm:ss}) {healthCheck.Name}");
                    }

                    if (healthCheck.Failed || healthCheckTimeout)
                    {
                        healthCheckFailed = true;
                    }
                }

                if (healthCheckFailed)
                {
                    healthCheckFailures++;
                }
                else
                {
                    healthCheckFailures = 0;
                }

                if (healthCheckFailed && coreService.Config.HealthCheckFailuresToRestartServices > 0 && healthCheckFailures >= coreService.Config.HealthCheckFailuresToRestartServices)
                {
                    coreService.Restart();
                }
                else
                {
                    if (healthCheckFailed && !tradingService.IsTradingSuspended)
                    {
                        loggingService.Info($"Health check failed ({healthCheckFailures})");
                        notificationService.Notify($"Health check failed ({healthCheckFailures})");
                        healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingPairsProcessed);
                        healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingRulesProcessed);
                        healthCheckService.RemoveHealthCheck(Constants.HealthChecks.SignalRulesProcessed);
                        tradingService.SuspendTrading();
                    }
                    else if (!healthCheckFailed && tradingService.IsTradingSuspended)
                    {
                        loggingService.Info("Health check passed");
                        notificationService.Notify("Health check passed");
                        tradingService.ResumeTrading();
                    }
                }
            }
        }
    }
}
