using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class BuyOptions
    {
        public string Pair { get; set; }
        public decimal? Amount { get; set; }
        public decimal? MaxCost { get; set; }
        public bool IgnoreExisting { get; set; }
        public bool ManualOrder { get; set; }
        public bool Swap { get; set; }
        public OrderMetadata Metadata { get; set; }

        public BuyOptions(string pair)
        {
            this.Pair = pair;
            this.Metadata = new OrderMetadata();
        }
    }
}
