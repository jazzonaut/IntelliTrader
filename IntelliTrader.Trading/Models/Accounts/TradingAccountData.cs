using IntelliTrader.Core;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace IntelliTrader.Trading
{
    internal class TradingAccountData
    {
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal Balance { get; set; }
        public ConcurrentDictionary<string, TradingPair> TradingPairs { get; set; }
    }
}
