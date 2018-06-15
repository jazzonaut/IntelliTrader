using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IntelliTrader.Core
{
    internal class HealthCheckService : IHealthCheckService
    {
        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;

        private readonly ConcurrentDictionary<string, HealthCheck> healthChecks = new ConcurrentDictionary<string, HealthCheck>();
        private HealthCheckTimedTask healthCheckTimedTask;

        public HealthCheckService(ILoggingService loggingService, INotificationService notificationService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
        }

        public void Start()
        {
            loggingService.Info($"Start Health Check service...");

            healthCheckTimedTask = new HealthCheckTimedTask(loggingService, notificationService, this, Application.Resolve<ICoreService>(), Application.Resolve<ITradingService>());
            healthCheckTimedTask.RunInterval = (float)(Application.Resolve<ICoreService>().Config.HealthCheckInterval * 1000 / Application.Speed);
            healthCheckTimedTask.StartDelay = Constants.TimedTasks.StandardDelay / Application.Speed;
            Application.Resolve<ICoreService>().AddTask(nameof(HealthCheckTimedTask), healthCheckTimedTask);

            loggingService.Info("Health Check service started");
        }

        public void Stop()
        {
            loggingService.Info($"Stop Health Check service...");

            Application.Resolve<ICoreService>().StopTask(nameof(HealthCheckTimedTask));
            Application.Resolve<ICoreService>().RemoveTask(nameof(HealthCheckTimedTask));

            loggingService.Info("Health Check service stopped");
        }

        public void UpdateHealthCheck(string name, string message = null, bool failed = false)
        {
            if (!healthChecks.TryGetValue(name, out HealthCheck existingHealthCheck))
            {
                healthChecks.TryAdd(name, new HealthCheck
                {
                    Name = name,
                    Message = message,
                    LastUpdated = DateTimeOffset.Now,
                    Failed = failed
                });
            }
            else
            {
                healthChecks[name].Message = message;
                healthChecks[name].LastUpdated = DateTimeOffset.Now;
                healthChecks[name].Failed = failed;
            }
        }

        public void RemoveHealthCheck(string name)
        {
            healthChecks.TryRemove(name, out HealthCheck healthCheck);
        }

        public IEnumerable<IHealthCheck> GetHealthChecks()
        {
            foreach (var kvp in healthChecks)
            {
                yield return kvp.Value;
            }
        }
    }
}
