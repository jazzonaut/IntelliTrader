using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITradingPair
    {
        string Pair { get; }
        string FormattedName { get; }
        int DCALevel { get; }
        List<string> OrderIds { get; }
        List<DateTimeOffset> OrderDates { get; }
        decimal Amount { get; }
        decimal AveragePrice { get; }
        decimal Fees { get; }
        decimal Cost { get; }
        decimal? CostOverride { get; set; }
        decimal CurrentCost { get; }
        decimal CurrentPrice { get; }
        decimal CurrentSpread { get; }
        decimal CurrentMargin { get; }
        double CurrentAge { get; }
        double LastBuyAge { get; }
        OrderMetadata Metadata { get; }

        decimal GetPartialCost(decimal partialAmount);
        void OverrideCost(decimal? costOverride);
        void SetCurrentValues(decimal currentPrice, decimal currentSpread);
        void SetMetadata(OrderMetadata metadata);
    }
}
