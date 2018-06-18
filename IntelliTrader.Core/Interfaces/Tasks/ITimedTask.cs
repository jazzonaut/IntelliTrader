using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITimedTask
    {
        double StartDelay { get; set; }
        double Interval { get; set; }
        bool IsRunning { get; }
        long RunCount { get; }
        double TotalRunTime { get; }
        double TotalLagTime { get; }

        void Start();
        void Stop();
        void Run();
    }
}
