using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITradeResult
    {
        bool IsSuccessful { get; }
        bool IsSwap { get; }
        bool IsArbitrage { get; }
        string Pair { get; }
        decimal Amount { get; }
        List<DateTimeOffset> OrderDates { get; }
        decimal AveragePrice { get; }
        decimal Fees { get; }
        decimal FeesTotal { get; }
        decimal ActualCost { get; }
        DateTimeOffset SellDate { get; }
        decimal SellPrice { get; }
        decimal SellCost { get; }
        decimal BalanceOffset { get; }
        decimal Profit { get; }
        OrderMetadata Metadata { get; }
    }
}
