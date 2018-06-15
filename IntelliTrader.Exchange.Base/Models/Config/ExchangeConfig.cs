using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Exchange.Base
{
    public class ExchangeConfig
    {
        public string KeysPath { get; set; }
        public int RateLimitOccurences { get; set; }
        public int RateLimitTimeframe { get; set; }
    }
}
