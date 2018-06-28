using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITradingConfig : IBuyConfig, IBuyDCAConfig, ISellConfig, ISellDCAConfig
    {
        bool Enabled { get; }
        string Market { get; }
        string Exchange { get; }
        int MaxPairs { get; }
        decimal MinCost { get; }
        List<string> ExcludedPairs { get; }
        TradePriceType TradePriceType { get; set; }

        bool RepeatLastDCALevel { get; }
        List<DCALevel> DCALevels { get; }

        double TradingCheckInterval { get; }
        double AccountRefreshInterval { get; }
        decimal AccountInitialBalance { get; }
        DateTimeOffset AccountInitialBalanceDate { get; }
        string AccountFilePath { get; }

        bool VirtualTrading { get; }
        decimal VirtualTradingFees { get; }
        decimal VirtualAccountInitialBalance { get; }
        string VirtualAccountFilePath { get; }

        ITradingConfig Clone();

    }
}
