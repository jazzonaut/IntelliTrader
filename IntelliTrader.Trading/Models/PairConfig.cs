using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    internal class PairConfig : IPairConfig
    {
        public IEnumerable<string> Rules { get; set; }

        public bool BuyEnabled { get; set; }
        public OrderType BuyType { get; set; }
        public decimal BuyMaxCost { get; set; }
        public decimal BuyMultiplier { get; set; }
        public decimal BuyMinBalance { get; set; }
        public double BuySamePairTimeout { get; set; }
        public decimal BuyTrailing { get; set; }
        public decimal BuyTrailingStopMargin { get; set; }
        public BuyTrailingStopAction BuyTrailingStopAction { get; set; }

        public bool SellEnabled { get; set; }
        public OrderType SellType { get; set; }
        public decimal SellMargin { get; set; }
        public decimal SellTrailing { get; set; }
        public decimal SellTrailingStopMargin { get; set; }
        public SellTrailingStopAction SellTrailingStopAction { get; set; }
        public bool SellStopLossEnabled { get; set; }
        public bool SellStopLossAfterDCA { get; set; }
        public double SellStopLossMinAge { get; set; }
        public decimal SellStopLossMargin { get; set; }

        public bool SwapEnabled { get; set; }
        public List<string> SwapSignalRules { get; set; }
        public int SwapTimeout { get; set; }

        public decimal? CurrentDCAMargin { get; set; }
        public decimal? NextDCAMargin { get; set; }
    }
}
