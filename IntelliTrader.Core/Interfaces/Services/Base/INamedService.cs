using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface INamedService
    {
        string ServiceName { get; }
    }
}
