using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITradeResult
    {
        bool IsSuccessful { get; }
        bool IsSwap { get; }
        string Pair { get; }
        decimal Amount { get; }
        List<DateTimeOffset> OrderDates { get; }
        decimal AveragePricePaid { get; }
        decimal FeesPairCurrency { get; }
        decimal FeesMarketCurrency { get; }
        decimal AverageCost { get; }
        DateTimeOffset SellDate { get; }
        decimal SellPrice { get; }
        decimal SellCost { get; }
        decimal BalanceDifference { get; }
        decimal Profit { get; }
        OrderMetadata Metadata { get; }

        void SetSwap(bool isSwap);
    }
}
