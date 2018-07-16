using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class ArbitrageOptions
    {
        public string Pair { get; set; }
        public ArbitrageMarket Market { get; set; }
        public bool ManualOrder { get; set; }
        public OrderMetadata Metadata { get; set; }

        public ArbitrageOptions(string pair, ArbitrageMarket market, OrderMetadata newPairMetadata)
        {
            this.Pair = pair;
            this.Market = market;
            this.Metadata = newPairMetadata ?? new OrderMetadata();
        }
    }
}
