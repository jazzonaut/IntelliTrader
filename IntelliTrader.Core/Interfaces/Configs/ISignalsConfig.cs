using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ISignalsConfig
    {
        bool Enabled { get; }
        IEnumerable<string> GlobalRatingSignals { get; }
        IEnumerable<ISignalDefinition> Definitions { get; }
    }
}
