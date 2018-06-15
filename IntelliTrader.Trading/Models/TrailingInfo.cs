using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    internal abstract class TrailingInfo
    {
        public decimal Trailing { get; set; }
        public decimal TrailingStopMargin { get; set; }
        public decimal InitialPrice { get; set; }
        public decimal LastTrailingMargin { get; set; }
        public decimal BestTrailingMargin { get; set; }
    }
}
