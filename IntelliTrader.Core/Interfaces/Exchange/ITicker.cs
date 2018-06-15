using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITicker
    {
        string Pair { get; }
        decimal BidPrice { get; }
        decimal AskPrice { get; }
        decimal LastPrice { get; }
    }
}
