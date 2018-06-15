using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    internal class CoreConfig : ICoreConfig
    {
        public bool DebugMode { get; set; }
        public bool PasswordProtected { get; set; }
        public string Password { get; set; }
        public string InstanceName { get; set; }
        public double TimezoneOffset { get; set; }
        public bool HealthCheckEnabled { get; set; } = true;
        public double HealthCheckInterval { get; set; }
        public double HealthCheckSuspendTradingTimeout { get; set; }
        public int HealthCheckFailuresToRestartServices { get; set; }
    }
}
