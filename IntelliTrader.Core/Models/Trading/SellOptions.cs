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
        public bool Arbitrage { get; set; }
        public OrderMetadata Metadata { get; set; }

        public SellOptions(string pair)
        {
            this.Pair = pair;
            this.Metadata = new OrderMetadata();
        }
    }
}
