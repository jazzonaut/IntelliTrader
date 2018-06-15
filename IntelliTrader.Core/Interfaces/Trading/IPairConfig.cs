using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IPairConfig : IBuyConfig, ISellConfig
    {
        IEnumerable<string> Rules { get; }

        bool SwapEnabled { get; }
        List<string> SwapSignalRules { get; }
        int SwapTimeout { get; }

        decimal? CurrentDCAMargin { get; }
        decimal? NextDCAMargin { get; }
    }
}
