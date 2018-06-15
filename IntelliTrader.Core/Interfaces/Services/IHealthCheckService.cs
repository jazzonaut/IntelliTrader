using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IHealthCheckService
    {
        void Start();
        void Stop();
        void UpdateHealthCheck(string name, string message = null, bool failed = false);
        void RemoveHealthCheck(string name);
        IEnumerable<IHealthCheck> GetHealthChecks();
    }
}
