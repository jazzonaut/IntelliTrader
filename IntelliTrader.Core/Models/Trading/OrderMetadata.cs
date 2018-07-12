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
        [JsonIgnore]
        public double? CurrentRating { get; set; }
        public double? BoughtGlobalRating { get; set; }
        [JsonIgnore]
        public double? CurrentGlobalRating { get; set; }
        public decimal? LastBuyMargin { get; set; }
        public int? AdditionalDCALevels { get; set; }
        public decimal? AdditionalCosts { get; set; }
        public string SwapPair { get; set; }
        public string ArbitrageMarket { get; set; }
        public decimal? ArbitragePercentage { get; set; }

        public void MergeWith(OrderMetadata metadata)
        {
            TradingRules = metadata.TradingRules ?? TradingRules;
            SignalRule = metadata.SignalRule ?? SignalRule;
            Signals = metadata.Signals ?? Signals;
            BoughtRating = metadata.BoughtRating ?? BoughtRating;
            CurrentRating = metadata.CurrentRating ?? CurrentRating;
            BoughtGlobalRating = metadata.BoughtGlobalRating ?? BoughtGlobalRating;
            CurrentGlobalRating = metadata.CurrentGlobalRating ?? CurrentGlobalRating;
            LastBuyMargin = metadata.LastBuyMargin ?? LastBuyMargin;
            AdditionalDCALevels = metadata.AdditionalDCALevels ?? AdditionalDCALevels;
            AdditionalCosts = metadata.AdditionalCosts ?? AdditionalCosts;
            SwapPair = metadata.SwapPair ?? SwapPair;
            ArbitrageMarket = metadata.ArbitrageMarket ?? ArbitrageMarket;
            ArbitragePercentage = metadata.ArbitragePercentage ?? ArbitragePercentage;
        }
    }
}
