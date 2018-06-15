using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ICoreConfig
    {
        bool DebugMode { get; }
        bool PasswordProtected { get; }
        string Password { get; }
        string InstanceName { get; }
        double TimezoneOffset { get; }
        bool HealthCheckEnabled { get; set; }
        double HealthCheckInterval { get; }
        double HealthCheckSuspendTradingTimeout { get; }
        int HealthCheckFailuresToRestartServices { get; }
    }
}
