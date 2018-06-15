using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Signals.Base
{
    public class SignalRulesTimedTask : HighResolutionTimedTask
    {
        public bool LoggingEnabled { get; set; } = true;

        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;
        private readonly IRulesService rulesService;
        private readonly ISignalsService signalsService;

        private ConcurrentDictionary<string, List<SignalTrailingInfo>> trailingSignals = new ConcurrentDictionary<string, List<SignalTrailingInfo>>();

        public SignalRulesTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService, IRulesService rulesService, ISignalsService signalsService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
            this.rulesService = rulesService;
            this.signalsService = signalsService;
        }

        public override void Run()
        {
            ProcessTrailingSignals();
            ProcessRules();
        }

        public void ClearTrailing()
        {
            trailingSignals.Clear();
        }

        public List<string> GetTrailingSignals()
        {
            return trailingSignals.Keys.ToList();
        }

        public IEnumerable<ISignalTrailingInfo> GetTrailingInfo(string pair)
        {
            if (trailingSignals.TryGetValue(pair, out List<SignalTrailingInfo> trailingInfo))
            {
                return trailingInfo;
            }
            else
            {
                return null;
            }
        }

        private void ProcessTrailingSignals()
        {
            double? globalRating = signalsService.GetGlobalRating();

            foreach (var kvp in trailingSignals)
            {
                string pair = kvp.Key;
                List<SignalTrailingInfo> trailingInfoList = kvp.Value;

                for (int i = trailingInfoList.Count - 1; i >= 0; i--)
                {
                    SignalTrailingInfo trailingInfo = trailingInfoList[i];

                    if (trailingInfo.Rule.Trailing.MaxDuration == 0 || trailingInfo.Duration <= trailingInfo.Rule.Trailing.MaxDuration / Application.Speed)
                    {
                        if (trailingInfo.Duration >= trailingInfo.Rule.Trailing.MinDuration / Application.Speed)
                        {
                            IEnumerable<ISignal> signalsByPair = signalsService.GetSignalsByPair(pair);
                            if (signalsByPair != null)
                            {
                                Dictionary<string, ISignal> signals = signalsByPair.ToDictionary(s => s.Name, s => s);
                                if (rulesService.CheckConditions(trailingInfo.Rule.Conditions, signals, globalRating, pair, null))
                                {
                                    IEnumerable<ISignal> ruleSignals = signals.Where(s => trailingInfo.Rule.Conditions.Any(c => c.Signal == s.Key)).Select(s => s.Value);
                                    InitiateBuy(pair, trailingInfo.Rule, ruleSignals);
                                }
                            }
                        }
                    }
                    else
                    {
                        trailingInfoList.RemoveAt(i);
                        if (trailingInfoList.Count == 0)
                        {
                            trailingSignals.TryRemove(pair, out trailingInfoList);
                        }
                        if (LoggingEnabled)
                        {
                            loggingService.Info($"Cancel trailing signal for {pair}. Rule: {trailingInfo.Rule.Name}, Reason: max duration reached");
                        }
                    }
                }
            }
        }

        private void ProcessRules()
        {
            if (tradingService.Config.BuyEnabled)
            {
                var allSignals = signalsService.GetAllSignals();
                if (allSignals != null)
                {
                    var enabledRules = signalsService.Rules.Entries.Where(r => r.Enabled);
                    if (enabledRules.Any())
                    {
                        var groupedSignals = allSignals.Where(s => tradingService.GetPairConfig(s.Pair).BuyEnabled).GroupBy(s => s.Pair).ToDictionary(g => g.Key, g => g.ToDictionary(s => s.Name, s => s));
                        double? globalRating = signalsService.GetGlobalRating();

                        var excludedPairs = tradingService.Config.ExcludedPairs
                            .Concat(tradingService.Account.GetTradingPairs().Select(p => p.Pair))
                            .Concat(tradingService.GetTrailingBuys()).ToList();

                        if (signalsService.RulesConfig.ProcessingMode == RuleProcessingMode.FirstMatch)
                        {
                            excludedPairs.AddRange(trailingSignals.Keys);
                        }

                        foreach (var rule in enabledRules)
                        {
                            foreach (var group in groupedSignals)
                            {
                                string pair = group.Key;
                                Dictionary<string, ISignal> signals = group.Value;
                                ITradingPair tradingPair = tradingService.Account.GetTradingPair(pair);
                                IEnumerable<IRuleCondition> conditions = rule.Trailing != null && rule.Trailing.Enabled ? rule.Trailing.StartConditions : rule.Conditions;
                                List<SignalTrailingInfo> trailingInfoList;

                                if (!excludedPairs.Contains(pair) && (!trailingSignals.TryGetValue(pair, out trailingInfoList) || !trailingInfoList.Any(t => t.Rule == rule)) &&
                                    rulesService.CheckConditions(conditions, signals, globalRating, pair, tradingPair))
                                {
                                    IEnumerable<ISignal> ruleSignals = signals.Where(s => conditions.Any(c => c.Signal == s.Key)).Select(s => s.Value);
                                    if (rule.Trailing != null && rule.Trailing.Enabled)
                                    {
                                        if (trailingInfoList == null)
                                        {
                                            trailingInfoList = new List<SignalTrailingInfo>();
                                            trailingSignals.TryAdd(pair, trailingInfoList);
                                        }

                                        trailingInfoList.Add(new SignalTrailingInfo
                                        {
                                            Rule = rule,
                                            StartTime = DateTimeOffset.Now
                                        });

                                        string ruleSignalsList = String.Join(", ", ruleSignals.Select(s => s.Name));
                                        if (LoggingEnabled)
                                        {
                                            loggingService.Info($"Start trailing signal for {pair}. Rule: {rule.Name}, Signals: {ruleSignalsList}");
                                        }
                                    }
                                    else
                                    {
                                        InitiateBuy(pair, rule, ruleSignals);
                                    }

                                    if (signalsService.RulesConfig.ProcessingMode == RuleProcessingMode.FirstMatch)
                                    {
                                        excludedPairs.Add(pair);
                                    }
                                }
                            }
                        }
                    }

                    healthCheckService.UpdateHealthCheck(Constants.HealthChecks.SignalRulesProcessed, $"Rules: {enabledRules.Count()}, Trailing signals: {trailingSignals.Count}");
                }
            }
        }

        private void InitiateBuy(string pair, IRule rule, IEnumerable<ISignal> ruleSignals)
        {
            trailingSignals.TryRemove(pair, out List<SignalTrailingInfo> trailingInfo);

            IPairConfig pairConfig = tradingService.GetPairConfig(pair);
            SignalRuleModifiers ruleModifiers = rule.GetModifiers<SignalRuleModifiers>();
            string ruleSignalsList = String.Join(", ", ruleSignals.Select(s => s.Name));
            if (LoggingEnabled)
            {
                loggingService.Info($"Initiate buy request for {pair}. Rule: {rule.Name}, Signals: {ruleSignalsList}");
            }

            var buyOptions = new BuyOptions(pair)
            {
                MaxCost = pairConfig.BuyMaxCost * pairConfig.BuyMultiplier * (ruleModifiers?.CostMultiplier ?? 1),
                Metadata = new OrderMetadata
                {
                    SignalRule = rule.Name,
                    Signals = ruleSignals.Select(s => s.Name).ToList(),
                    BoughtRating = ruleSignals.Any(s => s.Rating.HasValue) ? ruleSignals.Where(s => s.Rating.HasValue).Average(s => s.Rating) : null,
                    BoughtGlobalRating = signalsService.GetGlobalRating()
                }
            };

            tradingService.Buy(buyOptions);
        }
    }
}
