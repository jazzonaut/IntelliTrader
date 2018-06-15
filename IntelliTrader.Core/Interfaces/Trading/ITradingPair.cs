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
        decimal TotalAmount { get; }
        decimal AveragePricePaid { get; }
        decimal FeesPairCurrency { get; }
        decimal FeesMarketCurrency { get; }
        decimal AverageCostPaid { get; }
        decimal CurrentCost { get; }
        decimal CurrentPrice { get; }
        decimal CurrentMargin { get; }
        double CurrentAge { get; }
        double LastBuyAge { get; }
        OrderMetadata Metadata { get; }

        void SetCurrentPrice(decimal currentPrice);
    }
}
