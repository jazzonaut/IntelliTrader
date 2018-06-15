using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Rules
{
    internal class RulesConfig : IRulesConfig
    {
        public IEnumerable<ModuleRules> Modules { get; set; }
        IEnumerable<IModuleRules> IRulesConfig.Modules => Modules;
    }
}
