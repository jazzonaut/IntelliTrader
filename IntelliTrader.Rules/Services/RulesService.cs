using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Rules
{
    internal class RulesService : ConfigrableServiceBase<RulesConfig>, IRulesService
    {
        public override string ServiceName => Constants.ServiceNames.RulesService;

        IRulesConfig IRulesService.Config => Config;

        private readonly ILoggingService loggingService;
        private readonly List<Action> rulesChangeCallbacks = new List<Action>();

        public RulesService(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        public IModuleRules GetRules(string module)
        {
            IModuleRules moduleRules = Config.Modules.FirstOrDefault(m => m.Module == module);
            if (moduleRules != null)
            {
                return moduleRules;
            }
            else
            {
                throw new Exception($"Unable to find rules for {module}");
            }
        }

        public bool CheckConditions(IEnumerable<IRuleCondition> conditions, Dictionary<string, ISignal> signals, double? globalRating, string pair, ITradingPair tradingPair)
        {
            foreach (var condition in conditions)
            {
                ISignal signal = null;
                if (condition.Signal != null && signals.TryGetValue(condition.Signal, out ISignal s)) {
                    signal = s;
                }

                if (condition.MinVolume != null && (signal == null || signal.Volume == null || signal.Volume < condition.MinVolume) ||
                    condition.MaxVolume != null && (signal == null || signal.Volume == null || signal.Volume > condition.MaxVolume) ||
                    condition.MinVolumeChange != null && (signal == null || signal.VolumeChange == null || signal.VolumeChange < condition.MinVolumeChange) ||
                    condition.MaxVolumeChange != null && (signal == null || signal.VolumeChange == null || signal.VolumeChange > condition.MaxVolumeChange) ||
                    condition.MinPrice != null && (signal == null || signal.Price == null || signal.Price < condition.MinPrice) ||
                    condition.MaxPrice != null && (signal == null || signal.Price == null || signal.Price > condition.MaxPrice) ||
                    condition.MinPriceChange != null && (signal == null || signal.PriceChange == null || signal.PriceChange < condition.MinPriceChange) ||
                    condition.MaxPriceChange != null && (signal == null || signal.PriceChange == null || signal.PriceChange > condition.MaxPriceChange) ||
                    condition.MinRating != null && (signal == null || signal.Rating == null || signal.Rating < condition.MinRating) ||
                    condition.MaxRating != null && (signal == null || signal.Rating == null || signal.Rating > condition.MaxRating) ||
                    condition.MinRatingChange != null && (signal == null || signal.RatingChange == null || signal.RatingChange < condition.MinRatingChange) ||
                    condition.MaxRatingChange != null && (signal == null || signal.RatingChange == null || signal.RatingChange > condition.MaxRatingChange) ||
                    condition.MinVolatility != null && (signal == null || signal.Volatility == null || signal.Volatility < condition.MinVolatility) ||
                    condition.MaxVolatility != null && (signal == null || signal.Volatility == null || signal.Volatility > condition.MaxVolatility) ||
                    condition.MinGlobalRating != null && (globalRating == null || globalRating < condition.MinGlobalRating) ||
                    condition.MaxGlobalRating != null && (globalRating == null || globalRating > condition.MaxGlobalRating) ||
                    condition.Pairs != null && (pair == null || !condition.Pairs.Contains(pair)) || 

                    condition.MinAge != null && (tradingPair == null || tradingPair.CurrentAge < condition.MinAge / Application.Speed) ||
                    condition.MaxAge != null && (tradingPair == null || tradingPair.CurrentAge > condition.MaxAge / Application.Speed) ||
                    condition.MinLastBuyAge != null && (tradingPair == null || tradingPair.LastBuyAge < condition.MinLastBuyAge / Application.Speed) ||
                    condition.MaxLastBuyAge != null && (tradingPair == null || tradingPair.LastBuyAge > condition.MaxLastBuyAge / Application.Speed) ||
                    condition.MinMargin != null && (tradingPair == null || tradingPair.CurrentMargin < condition.MinMargin) ||
                    condition.MaxMargin != null && (tradingPair == null || tradingPair.CurrentMargin > condition.MaxMargin) ||
                    condition.MinMarginChange != null && (tradingPair == null || tradingPair.Metadata.LastBuyMargin == null || (tradingPair.CurrentMargin - tradingPair.Metadata.LastBuyMargin) < condition.MinMarginChange) ||
                    condition.MaxMarginChange != null && (tradingPair == null || tradingPair.Metadata.LastBuyMargin == null || (tradingPair.CurrentMargin - tradingPair.Metadata.LastBuyMargin) > condition.MaxMarginChange) ||
                    condition.MinAmount != null && (tradingPair == null || tradingPair.TotalAmount < condition.MinAmount) ||
                    condition.MaxAmount != null && (tradingPair == null || tradingPair.TotalAmount > condition.MaxAmount) ||
                    condition.MinCost != null && (tradingPair == null || tradingPair.CurrentCost < condition.MinCost) ||
                    condition.MaxCost != null && (tradingPair == null || tradingPair.CurrentCost > condition.MaxCost) ||
                    condition.MinDCALevel != null && (tradingPair == null || tradingPair.DCALevel < condition.MinDCALevel) ||
                    condition.MaxDCALevel != null && (tradingPair == null || tradingPair.DCALevel > condition.MaxDCALevel) ||
                    condition.SignalRules != null && (tradingPair == null || tradingPair.Metadata.SignalRule == null || !condition.SignalRules.Contains(tradingPair.Metadata.SignalRule)))
                {
                    return false;
                }
            }
            return true;
        }

        public void RegisterRulesChangeCallback(Action callback)
        {
            rulesChangeCallbacks.Add(callback);
        }

        public void UnregisterRulesChangeCallback(Action callback)
        {
            rulesChangeCallbacks.Remove(callback);
        }

        protected override void OnConfigReloaded()
        {
            foreach (var callback in rulesChangeCallbacks)
            {
                callback();
            }
        }
    }
}
