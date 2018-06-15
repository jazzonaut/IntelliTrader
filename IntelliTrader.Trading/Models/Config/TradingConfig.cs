using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Trading
{
    internal class TradingConfig : ITradingConfig
    {
        public bool Enabled { get; set; }
        public string Market { get; set; }
        public string Exchange { get; set; }
        public int MaxPairs { get; set; }
        public decimal MinCost { get; set; }
        public List<string> ExcludedPairs { get; set; }

        public bool BuyEnabled { get; set; }
        public OrderType BuyType { get; set; }
        public decimal BuyMaxCost { get; set; }
        public decimal BuyMultiplier { get; set; }
        public decimal BuyMinBalance { get; set; }
        public double BuySamePairTimeout { get; set; }
        public decimal BuyTrailing { get; set; }
        public decimal BuyTrailingStopMargin { get; set; }
        public BuyTrailingStopAction BuyTrailingStopAction { get; set; }

        public bool BuyDCAEnabled { get; set; }
        public decimal BuyDCAMultiplier { get; set; }
        public decimal BuyDCAMinBalance { get; set; }
        public double BuyDCASamePairTimeout { get; set; }
        public decimal BuyDCATrailing { get; set; }
        public decimal BuyDCATrailingStopMargin { get; set; }
        public BuyTrailingStopAction BuyDCATrailingStopAction { get; set; }

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

        public decimal SellDCAMargin { get; set; }
        public decimal SellDCATrailing { get; set; }
        public decimal SellDCATrailingStopMargin { get; set; }
        public SellTrailingStopAction SellDCATrailingStopAction { get; set; }

        public bool RepeatLastDCALevel { get; set; }
        public List<DCALevel> DCALevels { get; set; }

        public double TradingCheckInterval { get; set; }
        public double AccountRefreshInterval { get; set; }
        public decimal AccountInitialBalance { get; set; }
        public DateTimeOffset AccountInitialBalanceDate { get; set; }
        public string AccountFilePath { get; set; }

        public bool VirtualTrading { get; set; }
        public decimal VirtualAccountInitialBalance { get; set; }
        public string VirtualAccountFilePath { get; set; }

        public ITradingConfig Clone()
        {
            return new TradingConfig
            {
                Enabled = Enabled,
                Market = Market,
                Exchange = Exchange,
                MaxPairs = MaxPairs,
                MinCost = MinCost,
                ExcludedPairs = ExcludedPairs,

                BuyEnabled = BuyEnabled,
                BuyType = BuyType,
                BuyMaxCost = BuyMaxCost,
                BuyMultiplier = BuyMultiplier,
                BuyMinBalance = BuyMinBalance,
                BuySamePairTimeout = BuySamePairTimeout,
                BuyTrailing = BuyTrailing,
                BuyTrailingStopMargin = BuyTrailingStopMargin,
                BuyTrailingStopAction = BuyTrailingStopAction,

                BuyDCAEnabled = BuyDCAEnabled,
                BuyDCAMultiplier = BuyDCAMultiplier,
                BuyDCAMinBalance = BuyDCAMinBalance,
                BuyDCASamePairTimeout = BuyDCASamePairTimeout,
                BuyDCATrailing = BuyDCATrailing,
                BuyDCATrailingStopMargin = BuyDCATrailingStopMargin,
                BuyDCATrailingStopAction = BuyDCATrailingStopAction,

                SellEnabled = SellEnabled,
                SellType = SellType,
                SellMargin = SellMargin,
                SellTrailing = SellTrailing,
                SellTrailingStopMargin = SellTrailingStopMargin,
                SellTrailingStopAction = SellTrailingStopAction,
                SellStopLossEnabled = SellStopLossEnabled,
                SellStopLossAfterDCA = SellStopLossAfterDCA,
                SellStopLossMinAge = SellStopLossMinAge,
                SellStopLossMargin = SellStopLossMargin,

                SellDCAMargin = SellDCAMargin,
                SellDCATrailing = SellDCATrailing,
                SellDCATrailingStopMargin = SellDCATrailingStopMargin,
                SellDCATrailingStopAction = SellDCATrailingStopAction,

                RepeatLastDCALevel = RepeatLastDCALevel,
                DCALevels = DCALevels,

                TradingCheckInterval = TradingCheckInterval,
                AccountRefreshInterval = AccountRefreshInterval,
                AccountInitialBalance = AccountInitialBalance,
                AccountInitialBalanceDate = AccountInitialBalanceDate,
                AccountFilePath = AccountFilePath,

                VirtualTrading = VirtualTrading,
                VirtualAccountInitialBalance = VirtualAccountInitialBalance,
                VirtualAccountFilePath = VirtualAccountFilePath
            };
        }
    }
}