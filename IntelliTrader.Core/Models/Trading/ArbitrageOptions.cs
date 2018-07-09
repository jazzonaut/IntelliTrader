using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class ArbitrageOptions
    {
        public string Pair { get; set; }
        public string Market { get; set; }
        public bool ManualOrder { get; set; }
        public OrderMetadata Metadata { get; set; }

        public ArbitrageOptions(string pair, string market, OrderMetadata newPairMetadata)
        {
            this.Pair = pair;
            this.Market = market;
            this.Metadata = newPairMetadata ?? new OrderMetadata();
        }
    }
}
