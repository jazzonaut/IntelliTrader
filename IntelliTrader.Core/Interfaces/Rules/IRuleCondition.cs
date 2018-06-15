using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IRuleCondition
    {
        string Signal { get; }
        long? MinVolume { get; }
        long? MaxVolume { get; }
        double? MinVolumeChange { get; }
        double? MaxVolumeChange { get; }
        decimal? MinPrice { get; }
        decimal? MaxPrice { get; }
        decimal? MinPriceChange { get; }
        decimal? MaxPriceChange { get; }
        double? MinRating { get; }
        double? MaxRating { get; }
        double? MinRatingChange { get; }
        double? MaxRatingChange { get; }
        double? MinVolatility { get; }
        double? MaxVolatility { get; }
        double? MinGlobalRating { get; }
        double? MaxGlobalRating { get; }

        List<string> Pairs { get; }
        double? MinAge { get; }
        double? MaxAge { get; }
        double? MinLastBuyAge { get; }
        double? MaxLastBuyAge { get; }
        decimal? MinMargin { get; }
        decimal? MaxMargin { get; }
        decimal? MinMarginChange { get; }
        decimal? MaxMarginChange { get; }
        decimal? MinAmount { get; set; }
        decimal? MaxAmount { get; set; }
        decimal? MinCost { get; }
        decimal? MaxCost { get; }
        int? MinDCALevel { get; }
        int? MaxDCALevel { get; }
        List<string> SignalRules { get; }
    }
}
