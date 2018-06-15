using IntelliTrader.Core;
using System.Collections.Generic;

namespace IntelliTrader.Rules
{
    internal class RuleCondition : IRuleCondition
    {
        public string Signal { get; set; }
        public long? MinVolume { get; set; }
        public long? MaxVolume { get; set; }
        public double? MinVolumeChange { get; set; }
        public double? MaxVolumeChange { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinPriceChange { get; set; }
        public decimal? MaxPriceChange { get; set; }
        public double? MinRating { get; set; }
        public double? MaxRating { get; set; }
        public double? MinRatingChange { get; set; }
        public double? MaxRatingChange { get; set; }
        public double? MinVolatility { get; set; }
        public double? MaxVolatility { get; set; }
        public double? MinGlobalRating { get; set; }
        public double? MaxGlobalRating { get; set; }
        public List<string> Pairs { get; set; }

        // Trading pair specific conditions
        public double? MinAge { get; set; }
        public double? MaxAge { get; set; }
        public double? MinLastBuyAge { get; set; }
        public double? MaxLastBuyAge { get; set; }
        public decimal? MinMargin { get; set; }
        public decimal? MaxMargin { get; set; }
        public decimal? MinMarginChange { get; set; }
        public decimal? MaxMarginChange { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
        public int? MinDCALevel { get; set; }
        public int? MaxDCALevel { get; set; }
        public List<string> SignalRules { get; set; }
    }
}
