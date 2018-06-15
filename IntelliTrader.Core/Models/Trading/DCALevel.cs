using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class DCALevel
    {
        public decimal Margin { get; set; }
        public decimal? BuyMultiplier { get; set; }
        public double? BuySamePairTimeout { get; set; }
        public decimal? BuyTrailing { get; set; }
        public decimal? BuyTrailingStopMargin { get; set; }
        public BuyTrailingStopAction? BuyTrailingStopAction { get; set; }
        public decimal? SellMargin { get; set; }
        public decimal? SellTrailing { get; set; }
        public decimal? SellTrailingStopMargin { get; set; }
        public SellTrailingStopAction? SellTrailingStopAction { get; set; }
    }
}
