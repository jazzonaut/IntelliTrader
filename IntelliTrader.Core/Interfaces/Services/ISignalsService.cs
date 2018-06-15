using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ISignalsService : IConfigurableService
    {
        ISignalsConfig Config { get; }
        IModuleRules Rules { get; }
        ISignalRulesConfig RulesConfig { get; }
        void Start();
        void Stop();
        void ClearTrailing();
        List<string> GetTrailingSignals();
        IEnumerable<ISignalTrailingInfo> GetTrailingInfo(string pair);
        IEnumerable<string> GetSignalNames();
        IEnumerable<ISignal> GetAllSignals();
        IEnumerable<ISignal> GetSignalsByName(string signalName);
        IEnumerable<ISignal> GetSignalsByPair(string pair);
        double? GetRating(string pair, string signalName);
        double? GetRating(string pair, IEnumerable<string> signalNames);
        double? GetGlobalRating();
    }
}
