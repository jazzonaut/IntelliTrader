using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    internal class TradingRuleModifiers
    {
        public bool? BuyEnabled { get; set; }
        public decimal? BuyMaxCost { get; set; }
        public decimal? BuyMultiplier { get; set; }
        public decimal? BuyMinBalance { get; set; }
        public double? BuySamePairTimeout { get; set; }
        public decimal? BuyTrailing { get; set; }
        public decimal? BuyTrailingStopMargin { get; set; }
        public BuyTrailingStopAction? BuyTrailingStopAction { get; set; }

        public bool? BuyDCAEnabled { get; set; }
        public decimal? BuyDCAMultiplier { get; set; }
        public decimal? BuyDCAMinBalance { get; set; }
        public double? BuyDCASamePairTimeout { get; set; }
        public decimal? BuyDCATrailing { get; set; }
        public decimal? BuyDCATrailingStopMargin { get; set; }
        public BuyTrailingStopAction? BuyDCATrailingStopAction { get; set; }

        public bool? SellEnabled { get; set; }
        public decimal? SellMargin { get; set; }
        public decimal? SellTrailing { get; set; }
        public decimal? SellTrailingStopMargin { get; set; }
        public SellTrailingStopAction? SellTrailingStopAction { get; set; }
        public bool? SellStopLossEnabled { get; set; }
        public bool? SellStopLossAfterDCA { get; set; }
        public double? SellStopLossMinAge { get; set; }
        public decimal? SellStopLossMargin { get; set; }

        public decimal? SellDCAMargin { get; set; }
        public decimal? SellDCATrailing { get; set; }
        public decimal? SellDCATrailingStopMargin { get; set; }
        public SellTrailingStopAction? SellDCATrailingStopAction { get; set; }

        public bool? RepeatLastDCALevel { get; set; }
        public List<DCALevel> DCALevels { get; set; }

        public bool? SwapEnabled { get; set; }
        public List<string> SwapSignalRules { get; set; }
        public int? SwapTimeout { get; set; }
    }
}
