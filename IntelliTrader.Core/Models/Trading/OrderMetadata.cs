using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class OrderMetadata
    {
        public List<string> TradingRules { get; set; }
        public string SignalRule { get; set; }
        public List<string> Signals { get; set; }
        public double? BoughtRating { get; set; }
        public double? CurrentRating { get; set; }
        public double? BoughtGlobalRating { get; set; }
        public double? CurrentGlobalRating { get; set; }
        public decimal? LastBuyMargin { get; set; }
        public int? AdditionalDCALevels { get; set; }
        public decimal? AdditionalCosts { get; set; }
        public string SwapPair { get; set; }

        public OrderMetadata MergeWith(OrderMetadata metadata)
        {
            return new OrderMetadata
            {
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
                SwapPair = metadata.SwapPair ?? SwapPair
            };
        }
    }
}
