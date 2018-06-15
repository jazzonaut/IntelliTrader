using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IBuyDCAConfig
    {
        bool BuyDCAEnabled { get; set; }
        decimal BuyDCAMultiplier { get; }
        decimal BuyDCAMinBalance { get; }
        double BuyDCASamePairTimeout { get; }
        decimal BuyDCATrailing { get; }
        decimal BuyDCATrailingStopMargin { get; }
        BuyTrailingStopAction BuyDCATrailingStopAction { get; }
    }
}
