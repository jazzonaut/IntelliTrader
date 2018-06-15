using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IConfigurableService : INamedService
    {
        IConfigurationSection RawConfig { get; }
    }
}
