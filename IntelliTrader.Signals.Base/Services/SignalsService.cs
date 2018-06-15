using Autofac;
using IntelliTrader.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IntelliTrader.Signals.Base
{
    public class SignalsService : ConfigrableServiceBase<SignalsConfig>, ISignalsService
    {
        public override string ServiceName => Constants.ServiceNames.SignalsService;

        ISignalsConfig ISignalsService.Config => Config;

        public IModuleRules Rules { get; private set; }
        public ISignalRulesConfig RulesConfig { get; private set; }

        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;
        private readonly IRulesService rulesService;

        private ConcurrentDictionary<string, ISignalReceiver> signalReceivers = new ConcurrentDictionary<string, ISignalReceiver>();
        private SignalRulesTimedTask signalRulesTimedTask;

        public SignalsService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService, IRulesService rulesService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
            this.rulesService = rulesService;
        }

        public void Start()
        {
            loggingService.Info("Start Signals service...");

            OnSignalRulesChanged();
            rulesService.RegisterRulesChangeCallback(OnSignalRulesChanged);

            signalReceivers.Clear();
            foreach (var definition in Config.Definitions)
            {
                var receiver = Application.ResolveOptionalNamed<ISignalReceiver>(definition.Receiver,
                    new TypedParameter(typeof(string), definition.Name),
                    new TypedParameter(typeof(IConfigurationSection), definition.Configuration));

                if (receiver != null)
                {
                    if (signalReceivers.TryAdd(definition.Name, receiver))
                    {
                        receiver.Start();
                    }
                    else
                    {
                        throw new Exception($"Duplicate signal definition: {definition.Name}");
                    }
                }
                else
                {
                    throw new Exception($"Signal receiver not found: {definition.Receiver}");
                }
            }

            signalRulesTimedTask = new SignalRulesTimedTask(loggingService, healthCheckService, tradingService, rulesService, this);
            signalRulesTimedTask.RunInterval = (float)(RulesConfig.CheckInterval * 1000 / Application.Speed);
            signalRulesTimedTask.StartDelay = Constants.TimedTasks.StandardDelay / Application.Speed;
            Application.Resolve<ICoreService>().AddTask(nameof(SignalRulesTimedTask), signalRulesTimedTask);

            loggingService.Info("Signals service started");
        }

        public void Stop()
        {
            loggingService.Info("Stop Signals service...");

            foreach (var receiver in signalReceivers.Values)
            {
                receiver.Stop();
            }
            signalReceivers.Clear();

            Application.Resolve<ICoreService>().StopTask(nameof(SignalRulesTimedTask));
            Application.Resolve<ICoreService>().RemoveTask(nameof(SignalRulesTimedTask));

            rulesService.UnregisterRulesChangeCallback(OnSignalRulesChanged);

            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.SignalRulesProcessed);

            loggingService.Info("Signals service stopped");
        }

        public void ClearTrailing()
        {
            signalRulesTimedTask.ClearTrailing();
        }

        public List<string> GetTrailingSignals()
        {
            return signalRulesTimedTask.GetTrailingSignals();
        }

        public IEnumerable<ISignalTrailingInfo> GetTrailingInfo(string pair)
        {
            return signalRulesTimedTask.GetTrailingInfo(pair);
        }

        public IEnumerable<string> GetSignalNames()
        {
            return signalReceivers.OrderBy(r => r.Value.GetPeriod()).Select(r => r.Key);
        }

        public IEnumerable<ISignal> GetAllSignals()
        {
            return GetSignalsByName(null);
        }

        public IEnumerable<ISignal> GetSignalsByName(string signalName)
        {
            IEnumerable<ISignal> signals = null;
            foreach (var kvp in signalReceivers.OrderBy(r => r.Value.GetPeriod()))
            {
                if (signalName == null || signalName == kvp.Key)
                {
                    ISignalReceiver receiver = kvp.Value;
                    if (signals == null)
                    {
                        signals = receiver.GetSignals();
                    }
                    else
                    {
                        signals = signals.Concat(receiver.GetSignals());
                    }
                }
            }
            return signals;
        }

        public IEnumerable<ISignal> GetSignalsByPair(string pair)
        {
            foreach (var receiver in signalReceivers.Values.OrderBy(r => r.GetPeriod()))
            {
                var signal = receiver.GetSignals().FirstOrDefault(s => s.Pair == pair);
                if (signal != null)
                {
                    yield return signal;
                }
            }
        }

        public ISignal GetSignal(string pair, string signalName)
        {
            return GetSignalsByName(signalName)?.FirstOrDefault(s => s.Pair == pair);
        }

        public double? GetRating(string pair, string signalName)
        {
            return GetSignalsByName(signalName)?.FirstOrDefault(s => s.Pair == pair)?.Rating;
        }

        public double? GetRating(string pair, IEnumerable<string> signalNames)
        {
            if (signalNames != null && signalNames.Count() > 0)
            {
                double ratingSum = 0;

                foreach (var signalName in signalNames)
                {
                    var rating = GetSignalsByName(signalName)?.FirstOrDefault(s => s.Pair == pair)?.Rating;
                    if (rating != null)
                    {
                        ratingSum += rating.Value;
                    }
                    else
                    {
                        return null;
                    }
                }

                return Math.Round(ratingSum / signalNames.Count(), 8);
            }
            else
            {
                return null;
            }
        }

        public double? GetGlobalRating()
        {
            try
            {
                double ratingSum = 0;
                double ratingCount = 0;

                foreach (var kvp in signalReceivers)
                {
                    string signalName = kvp.Key;
                    if (Config.GlobalRatingSignals.Contains(signalName))
                    {
                        ISignalReceiver receiver = kvp.Value;
                        double? averageRating = receiver.GetAverageRating();
                        if (averageRating != null)
                        {
                            ratingSum += averageRating.Value;
                            ratingCount++;
                        }
                    }
                }

                if (ratingCount > 0)
                {
                    return Math.Round(ratingSum / ratingCount, 8);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                loggingService.Error("Unable to get global rating", ex);
                return null;
            }
        }

        private void OnSignalRulesChanged()
        {
            Rules = rulesService.GetRules(ServiceName);
            RulesConfig = Rules.GetConfiguration<SignalRulesConfig>();
        }
    }
}
