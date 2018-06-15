using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class SwapOptions
    {
        public string OldPair { get; set; }
        public string NewPair { get; set; }
        public bool ManualOrder { get; set; }
        public OrderMetadata Metadata { get; set; }

        public SwapOptions(string oldPair, string newPair, OrderMetadata newPairMetadata)
        {
            this.OldPair = oldPair;
            this.NewPair = newPair;
            this.Metadata = newPairMetadata ?? new OrderMetadata();
        }
    }
}
