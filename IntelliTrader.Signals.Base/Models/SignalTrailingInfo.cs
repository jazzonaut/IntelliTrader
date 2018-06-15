using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Signals.Base
{
    internal class SignalTrailingInfo : ISignalTrailingInfo
    {
        public IRule Rule { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public double Duration => (DateTimeOffset.Now - StartTime).TotalSeconds;
    }
}
