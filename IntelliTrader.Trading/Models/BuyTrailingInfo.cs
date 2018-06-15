using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    internal class BuyTrailingInfo : TrailingInfo
    {
        public BuyOptions BuyOptions { get; set; }
        public BuyTrailingStopAction TrailingStopAction { get; set; }
    }
}
