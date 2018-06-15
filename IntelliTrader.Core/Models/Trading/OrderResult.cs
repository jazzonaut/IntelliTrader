using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public enum OrderResult
    {
        Unknown = 0,
        Filled = 1,
        FilledPartially = 2,
        Pending = 3,
        Error = 4,
        Canceled = 5
    }
}
