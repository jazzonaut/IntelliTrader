using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class SellOptions
    {
        public string Pair { get; set; }
        public decimal? Amount { get; set; }
        public bool ManualOrder { get; set; }
        public bool Swap { get; set; }
        public string SwapPair { get; set; }

        public SellOptions(string pair)
        {
            this.Pair = pair;
        }
    }
}
