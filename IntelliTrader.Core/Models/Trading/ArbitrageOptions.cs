using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class ArbitrageOptions
    {
        public string Pair { get; set; }
        public Arbitrage Arbitrage { get; set; }
        public bool ManualOrder { get; set; }
        public OrderMetadata Metadata { get; set; }

        public ArbitrageOptions(string pair, Arbitrage arbitrage, OrderMetadata newPairMetadata)
        {
            this.Pair = pair;
            this.Arbitrage = arbitrage;
            this.Metadata = newPairMetadata ?? new OrderMetadata();
        }
    }
}
