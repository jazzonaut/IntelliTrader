using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IRulesService : IConfigurableService
    {
        IRulesConfig Config { get; }
        IModuleRules GetRules(string module);
        bool CheckConditions(IEnumerable<IRuleCondition> conditions, Dictionary<string, ISignal> signals, double? globalRating, string pair, ITradingPair tradingPair);
        void RegisterRulesChangeCallback(Action callback);
        void UnregisterRulesChangeCallback(Action callback);
    }
}
