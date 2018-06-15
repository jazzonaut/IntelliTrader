using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IHealthCheck
    {
        string Name { get; }
        string Message { get; }
        DateTimeOffset LastUpdated { get; }
        bool Failed { get; }
    }
}
