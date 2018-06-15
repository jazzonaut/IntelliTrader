using IntelliTrader.Core;
using System;
using System.Collections.Generic;

namespace IntelliTrader.Core
{
    public class TradeResult : ITradeResult
    {
        public bool IsSuccessful { get; set; }
        public bool IsSwap { get; set; }
        public string Pair { get; set; }
        public decimal Amount { get; set; }
        public List<DateTimeOffset> OrderDates { get; set; }
        public decimal AveragePricePaid { get; set; }
        public decimal FeesPairCurrency { get; set; }
        public decimal FeesMarketCurrency { get; set; }
        public decimal AverageCost => AveragePricePaid * (Amount + FeesPairCurrency) + FeesMarketCurrency;
        public DateTimeOffset SellDate { get; set; }
        public decimal SellPrice { get; set; }
        public decimal SellCost => SellPrice * Amount;
        public decimal BalanceDifference { get; set; }
        public decimal Profit { get; set; }
        public OrderMetadata Metadata { get; set; }

        public void SetSwap(bool isSwap)
        {
            this.IsSwap = isSwap;
        }
    }
}
