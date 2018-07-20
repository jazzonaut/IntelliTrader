using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class OrderMetadata
    {
        public bool? IsTransitional { get; set; }
        public string OriginalPair { get; set; }
        public List<string> TradingRules { get; set; }
        public string SignalRule { get; set; }
        public List<string> Signals { get; set; }
        public double? BoughtRating { get; set; }
        [JsonIgnore]
        public double? CurrentRating { get; set; }
        public double? BoughtGlobalRating { get; set; }
        [JsonIgnore]
        public double? CurrentGlobalRating { get; set; }
        public decimal? LastBuyMargin { get; set; }
        public int? AdditionalDCALevels { get; set; }
        public decimal? AdditionalCosts { get; set; }
        public decimal? FeesNonDeductible { get; set; }
        public string SwapPair { get; set; }
        public string Arbitrage { get; set; }
        public decimal? ArbitragePercentage { get; set; }

        public OrderMetadata MergeWith(OrderMetadata metadata)
        {
            return new OrderMetadata
            {
                IsTransitional = metadata.IsTransitional ?? IsTransitional,
                OriginalPair = metadata.OriginalPair ?? OriginalPair,
                TradingRules = metadata.TradingRules ?? TradingRules,
                SignalRule = metadata.SignalRule ?? SignalRule,
                Signals = metadata.Signals ?? Signals,
                BoughtRating = metadata.BoughtRating ?? BoughtRating,
                CurrentRating = metadata.CurrentRating ?? CurrentRating,
                BoughtGlobalRating = metadata.BoughtGlobalRating ?? BoughtGlobalRating,
                CurrentGlobalRating = metadata.CurrentGlobalRating ?? CurrentGlobalRating,
                LastBuyMargin = metadata.LastBuyMargin ?? LastBuyMargin,
                AdditionalDCALevels = metadata.AdditionalDCALevels ?? AdditionalDCALevels,
                AdditionalCosts = metadata.AdditionalCosts ?? AdditionalCosts,
                FeesNonDeductible = metadata.FeesNonDeductible ?? FeesNonDeductible,
                SwapPair = metadata.SwapPair ?? SwapPair,
                Arbitrage = metadata.Arbitrage ?? Arbitrage,
                ArbitragePercentage = metadata.ArbitragePercentage ?? ArbitragePercentage
            };
        }
    }
}
