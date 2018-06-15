using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ISellDCAConfig
    {
        decimal SellDCAMargin { get; }
        decimal SellDCATrailing { get; }
        decimal SellDCATrailingStopMargin { get; }
        SellTrailingStopAction SellDCATrailingStopAction { get; }
    }
}
