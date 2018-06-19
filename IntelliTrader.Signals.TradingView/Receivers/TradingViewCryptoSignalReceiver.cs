using IntelliTrader.Core;
using IntelliTrader.Signals.Base;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace IntelliTrader.Signals.TradingView
{
    internal class TradingViewCryptoSignalReceiver : ISignalReceiver
    {
        public string SignalName { get; private set; }
        public TradingViewCryptoSignalReceiverConfig Config { get; private set; }
        
        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ISignalsService signalsService;
        private readonly ITradingService tradingService;

        private TradingViewCryptoSignalPollingTimedTask tradingViewCryptoSignalPollingTimedTask;

        public TradingViewCryptoSignalReceiver(string signalName, IConfigurationSection configuration, 
            ILoggingService loggingService, IHealthCheckService healthCheckService, ISignalsService signalsService, ITradingService tradingService)
        {
            this.SignalName = signalName;
            this.Config = configuration.Get<TradingViewCryptoSignalReceiverConfig>();

            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.signalsService = signalsService;
            this.tradingService = tradingService;
        }

        public void Start()
        {
            loggingService.Info("Start TradingViewCryptoSignalReceiver...");

            tradingViewCryptoSignalPollingTimedTask = Application.Resolve<ITasksService>().AddTask(
                name: $"{nameof(TradingViewCryptoSignalPollingTimedTask)} [{SignalName}]",
                task: new TradingViewCryptoSignalPollingTimedTask(loggingService, healthCheckService, tradingService, this),
                interval: Config.PollingInterval * 1000 / Application.Speed,
                startDelay: Constants.TaskDelays.ZeroDelay,
                startTask: false,
                runNow: true);

            loggingService.Info("TradingViewCryptoSignalReceiver started");
        }

        public void Stop()
        {
            loggingService.Info("Stop TradingViewCryptoSignalReceiver...");

            Application.Resolve<ITasksService>().RemoveTask($"{nameof(TradingViewCryptoSignalPollingTimedTask)} [{SignalName}]", stopTask: true);

            healthCheckService.RemoveHealthCheck($"{Constants.HealthChecks.TradingViewCryptoSignalsReceived} [{SignalName}]");

            loggingService.Info("TradingViewCryptoSignalReceiver stopped");
        }

        public int GetPeriod()
        {
            return Config.SignalPeriod;
        }

        public IEnumerable<ISignal> GetSignals()
        {
            return tradingViewCryptoSignalPollingTimedTask?.GetSignals() ?? new List<ISignal>();
        }

        public double? GetAverageRating()
        {
            return tradingViewCryptoSignalPollingTimedTask?.GetAverageRating();
        }
    }
}
