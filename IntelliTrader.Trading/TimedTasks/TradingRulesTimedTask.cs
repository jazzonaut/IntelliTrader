using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IntelliTrader.Trading
{
    public class TradingRulesTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly IRulesService rulesService;
        private readonly ISignalsService signalsService;
        private readonly TradingService tradingService;

        private readonly ConcurrentDictionary<string, PairConfig> pairConfigs = new ConcurrentDictionary<string, PairConfig>();

        public TradingRulesTimedTask(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, IRulesService rulesService, ISignalsService signalsService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.rulesService = rulesService;
            this.signalsService = signalsService;
            this.tradingService = tradingService as TradingService;
        }

        protected override void Run()
        {
            ProcessAllRules();
        }

        public IPairConfig GetPairConfig(string pair)
        {
            PairConfig pairConfig;
            if (!pairConfigs.TryGetValue(pair, out pairConfig))
            {
                ProcessAllRules();
                if (!pairConfigs.TryGetValue(pair, out pairConfig))
                {
                    return CreatePairConfig(pair, tradingService.Config.Clone(), new PairConfig(), new List<IRule>());
                }
            }
            return pairConfig;
        }

        public void ProcessAllRules()
        {
            IEnumerable<IRule> enabledRules = tradingService.Rules?.Entries?.Where(r => r.Enabled) ?? new List<IRule>();
            List<string> allPairs = tradingService.Exchange.GetMarketPairs(tradingService.Config.Market).ToList();
            double? globalRating = signalsService.GetGlobalRating();

            foreach (string pair in allPairs)
            {
                IEnumerable<ISignal> signalsByPair = signalsService.GetSignalsByPair(pair);
                if (signalsByPair != null)
                {
                    Dictionary<string, ISignal> signals = signalsByPair.ToDictionary(s => s.Name, s => s);
                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(pair);
                    TradingConfig modifiedTradingConfig = tradingService.Config.Clone() as TradingConfig;
                    PairConfig modifiedPairConfig = new PairConfig();
                    pairConfigs.TryGetValue(pair, out PairConfig oldPairConfig);
                    var appliedRules = new List<IRule>();

                    foreach (var rule in enabledRules)
                    {
                        if (rulesService.CheckConditions(rule.Conditions, signals, globalRating, pair, tradingPair))
                        {
                            var modifiers = rule.GetModifiers<TradingRuleModifiers>();
                            if (modifiers != null)
                            {
                                // Base Trading Config
                                modifiedTradingConfig.MaxPairs = modifiers.MaxPairs ?? modifiedTradingConfig.MaxPairs;
                                modifiedTradingConfig.BuyEnabled = modifiers.BuyEnabled ?? modifiedTradingConfig.BuyEnabled;
                                modifiedTradingConfig.BuyMaxCost = modifiers.BuyMaxCost ?? modifiedTradingConfig.BuyMaxCost;
                                modifiedTradingConfig.BuyMultiplier = modifiers.BuyMultiplier ?? modifiedTradingConfig.BuyMultiplier;
                                modifiedTradingConfig.BuyMinBalance = modifiers.BuyMinBalance ?? modifiedTradingConfig.BuyMinBalance;
                                modifiedTradingConfig.BuySamePairTimeout = modifiers.BuySamePairTimeout ?? modifiedTradingConfig.BuySamePairTimeout;
                                modifiedTradingConfig.BuyTrailing = modifiers.BuyTrailing ?? modifiedTradingConfig.BuyTrailing;
                                modifiedTradingConfig.BuyTrailingStopMargin = modifiers.BuyTrailingStopMargin ?? modifiedTradingConfig.BuyTrailingStopMargin;
                                modifiedTradingConfig.BuyTrailingStopAction = modifiers.BuyTrailingStopAction ?? modifiedTradingConfig.BuyTrailingStopAction;

                                modifiedTradingConfig.BuyDCAEnabled = modifiers.BuyDCAEnabled ?? modifiedTradingConfig.BuyDCAEnabled;
                                modifiedTradingConfig.BuyDCAMultiplier = modifiers.BuyDCAMultiplier ?? modifiedTradingConfig.BuyDCAMultiplier;
                                modifiedTradingConfig.BuyDCAMinBalance = modifiers.BuyDCAMinBalance ?? modifiedTradingConfig.BuyDCAMinBalance;
                                modifiedTradingConfig.BuyDCASamePairTimeout = modifiers.BuyDCASamePairTimeout ?? modifiedTradingConfig.BuyDCASamePairTimeout;
                                modifiedTradingConfig.BuyDCATrailing = modifiers.BuyDCATrailing ?? modifiedTradingConfig.BuyDCATrailing;
                                modifiedTradingConfig.BuyDCATrailingStopMargin = modifiers.BuyDCATrailingStopMargin ?? modifiedTradingConfig.BuyDCATrailingStopMargin;
                                modifiedTradingConfig.BuyDCATrailingStopAction = modifiers.BuyDCATrailingStopAction ?? modifiedTradingConfig.BuyDCATrailingStopAction;

                                modifiedTradingConfig.SellEnabled = modifiers.SellEnabled ?? modifiedTradingConfig.SellEnabled;
                                modifiedTradingConfig.SellMargin = modifiers.SellMargin ?? modifiedTradingConfig.SellMargin;
                                modifiedTradingConfig.SellTrailing = modifiers.SellTrailing ?? modifiedTradingConfig.SellTrailing;
                                modifiedTradingConfig.SellTrailingStopMargin = modifiers.SellTrailingStopMargin ?? modifiedTradingConfig.SellTrailingStopMargin;
                                modifiedTradingConfig.SellTrailingStopAction = modifiers.SellTrailingStopAction ?? modifiedTradingConfig.SellTrailingStopAction;
                                modifiedTradingConfig.SellStopLossEnabled = modifiers.SellStopLossEnabled ?? modifiedTradingConfig.SellStopLossEnabled;
                                modifiedTradingConfig.SellStopLossAfterDCA = modifiers.SellStopLossAfterDCA ?? modifiedTradingConfig.SellStopLossAfterDCA;
                                modifiedTradingConfig.SellStopLossMinAge = modifiers.SellStopLossMinAge ?? modifiedTradingConfig.SellStopLossMinAge;
                                modifiedTradingConfig.SellStopLossMargin = modifiers.SellStopLossMargin ?? modifiedTradingConfig.SellStopLossMargin;

                                modifiedTradingConfig.SellDCAMargin = modifiers.SellDCAMargin ?? modifiedTradingConfig.SellDCAMargin;
                                modifiedTradingConfig.SellDCATrailing = modifiers.SellDCATrailing ?? modifiedTradingConfig.SellDCATrailing;
                                modifiedTradingConfig.SellDCATrailingStopMargin = modifiers.SellDCATrailingStopMargin ?? modifiedTradingConfig.SellDCATrailingStopMargin;
                                modifiedTradingConfig.SellDCATrailingStopAction = modifiers.SellDCATrailingStopAction ?? modifiedTradingConfig.SellDCATrailingStopAction;

                                modifiedTradingConfig.RepeatLastDCALevel = modifiers.RepeatLastDCALevel ?? modifiedTradingConfig.RepeatLastDCALevel;
                                modifiedTradingConfig.DCALevels = modifiers.DCALevels ?? modifiedTradingConfig.DCALevels;

                                // Base Pair Config
                                modifiedPairConfig.SwapEnabled = modifiers.SwapEnabled ?? modifiedPairConfig.SwapEnabled;
                                modifiedPairConfig.SwapSignalRules = modifiers.SwapSignalRules ?? modifiedPairConfig.SwapSignalRules;
                                modifiedPairConfig.SwapTimeout = modifiers.SwapTimeout ?? modifiedPairConfig.SwapTimeout;

                                modifiedPairConfig.ArbitrageEnabled = modifiers.ArbitrageEnabled ?? modifiedPairConfig.ArbitrageEnabled;
                                modifiedPairConfig.ArbitrageMarket = modifiers.ArbitrageMarket ?? modifiedPairConfig.ArbitrageMarket;
                                modifiedPairConfig.ArbitrageSignalRules = modifiers.ArbitrageSignalRules ?? modifiedPairConfig.ArbitrageSignalRules;

                                if (oldPairConfig != null && !oldPairConfig.ArbitrageEnabled && modifiedPairConfig.ArbitrageEnabled)
                                {
                                    signalsService.ProcessPair(pair, signals);
                                }
                            }

                            appliedRules.Add(rule);

                            if (tradingService.RulesConfig.ProcessingMode == RuleProcessingMode.FirstMatch)
                            {
                                break;
                            }
                        }
                    }

                    pairConfigs[pair] = CreatePairConfig(pair, modifiedTradingConfig, modifiedPairConfig, appliedRules);
                }
            }

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TradingRulesProcessed, $"Rules: {enabledRules.Count()}, Pairs: {allPairs.Count}");
        }

        private PairConfig CreatePairConfig(string pair, ITradingConfig modifiedTradingConfig, IPairConfig modifiedPairConfig, IEnumerable<IRule> appliedRules)
        {
            ITradingPair tradingPair = tradingService.Account.GetTradingPair(pair);
            DCALevel currentDCALevel = GetCurrentDCALevel(tradingPair, modifiedTradingConfig.DCALevels);
            DCALevel nextDCALevel = GetNextDCALevel(tradingPair, modifiedTradingConfig.DCALevels, modifiedTradingConfig.RepeatLastDCALevel);

            return new PairConfig
            {
                Rules = appliedRules.Select(r => r.Name),

                MaxPairs = modifiedTradingConfig.MaxPairs,

                BuyEnabled = tradingPair == null ? modifiedTradingConfig.BuyEnabled : modifiedTradingConfig.BuyDCAEnabled,
                BuyType = modifiedTradingConfig.BuyType,
                BuyMaxCost = modifiedTradingConfig.BuyMaxCost,
                BuyMultiplier = tradingPair == null ? (modifiedTradingConfig.BuyMultiplier != 0 ? modifiedTradingConfig.BuyMultiplier : 1) : nextDCALevel?.BuyMultiplier ?? modifiedTradingConfig.BuyDCAMultiplier,
                BuyMinBalance = tradingPair == null ? modifiedTradingConfig.BuyMinBalance : modifiedTradingConfig.BuyDCAMinBalance,
                BuySamePairTimeout = (tradingPair == null ? modifiedTradingConfig.BuySamePairTimeout : nextDCALevel?.BuySamePairTimeout ?? modifiedTradingConfig.BuyDCASamePairTimeout) / Application.Speed,
                BuyTrailing = tradingPair == null ? modifiedTradingConfig.BuyTrailing : nextDCALevel?.BuyTrailing ?? modifiedTradingConfig.BuyDCATrailing,
                BuyTrailingStopMargin = tradingPair == null ? modifiedTradingConfig.BuyTrailingStopMargin : nextDCALevel?.BuyTrailingStopMargin ?? modifiedTradingConfig.BuyDCATrailingStopMargin,
                BuyTrailingStopAction = tradingPair == null ? modifiedTradingConfig.BuyTrailingStopAction : nextDCALevel?.BuyTrailingStopAction ?? modifiedTradingConfig.BuyDCATrailingStopAction,

                SellEnabled = modifiedTradingConfig.SellEnabled,
                SellType = modifiedTradingConfig.SellType,
                SellMargin = currentDCALevel == null ? modifiedTradingConfig.SellMargin : currentDCALevel?.SellMargin ?? modifiedTradingConfig.SellDCAMargin,
                SellTrailing = currentDCALevel == null ? modifiedTradingConfig.SellTrailing : currentDCALevel?.SellTrailing ?? modifiedTradingConfig.SellDCATrailing,
                SellTrailingStopMargin = currentDCALevel == null ? modifiedTradingConfig.SellTrailingStopMargin : currentDCALevel?.SellTrailingStopMargin ?? modifiedTradingConfig.SellDCATrailingStopMargin,
                SellTrailingStopAction = currentDCALevel == null ? modifiedTradingConfig.SellTrailingStopAction : currentDCALevel?.SellTrailingStopAction ?? modifiedTradingConfig.SellDCATrailingStopAction,
                SellStopLossEnabled = modifiedTradingConfig.SellStopLossEnabled,
                SellStopLossAfterDCA = modifiedTradingConfig.SellStopLossAfterDCA,
                SellStopLossMinAge = modifiedTradingConfig.SellStopLossMinAge / Application.Speed,
                SellStopLossMargin = modifiedTradingConfig.SellStopLossMargin,

                SwapEnabled = modifiedPairConfig.SwapEnabled,
                SwapSignalRules = modifiedPairConfig.SwapSignalRules,
                SwapTimeout = (int)Math.Round(modifiedPairConfig.SwapTimeout / Application.Speed),

                ArbitrageEnabled = modifiedPairConfig.ArbitrageEnabled,
                ArbitrageMarket = modifiedPairConfig.ArbitrageMarket,
                ArbitrageType = modifiedPairConfig.ArbitrageType,
                ArbitrageBuyMultiplier = modifiedPairConfig.ArbitrageBuyMultiplier,
                ArbitrageSignalRules = modifiedPairConfig.ArbitrageSignalRules,

                CurrentDCAMargin = currentDCALevel?.Margin,
                NextDCAMargin = nextDCALevel?.Margin
            };
        }

        private DCALevel GetCurrentDCALevel(ITradingPair tradingPair, List<DCALevel> dcaLevels)
        {
            if (tradingPair != null && tradingPair.DCALevel > 0 && dcaLevels.Count >= tradingPair.DCALevel)
            {
                return dcaLevels[tradingPair.DCALevel - 1];
            }
            else
            {
                return null;
            }
        }

        private DCALevel GetNextDCALevel(ITradingPair tradingPair, List<DCALevel> dcaLevels, bool repeatLastDCALevel)
        {
            if (tradingPair != null && dcaLevels.Count > 0)
            {
                if (dcaLevels.Count >= tradingPair.DCALevel + 1)
                {
                    return dcaLevels[tradingPair.DCALevel];
                }
                else if (repeatLastDCALevel)
                {
                    return dcaLevels[dcaLevels.Count - 1];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
