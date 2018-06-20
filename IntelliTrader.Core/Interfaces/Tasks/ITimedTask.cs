using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITimedTask
    {
        event UnhandledExceptionEventHandler UnhandledException;

        double StartDelay { get; set; }
        double Interval { get; set; }
        int SkipIteration { get; set; }
        Stopwatch Stopwatch { get; set; }
        bool IsRunning { get; }
        long RunCount { get; }
        double TotalRunTime { get; }
        double TotalLagTime { get; }

        void Start();
        void Stop();
        void RunNow();
    }
}
