using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IRulesConfig
    {
        IEnumerable<IModuleRules> Modules { get; }
    }
}
