using IntelliTrader.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Rules
{
    internal class ModuleRules : IModuleRules
    {
        public string Module { get; set; }
        public IConfigurationSection Configuration { get; set; }
        public IEnumerable<Rule> Entries { get; set; }
        IEnumerable<IRule> IModuleRules.Entries => Entries;

        public T GetConfiguration<T>()
        {
            return Configuration.Get<T>();
        }
    }
}