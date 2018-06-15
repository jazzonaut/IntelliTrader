using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    internal class HealthCheck : IHealthCheck
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public bool Failed { get; set; }
    }
}
