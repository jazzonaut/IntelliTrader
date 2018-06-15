using IntelliTrader.Core;
using Microsoft.Extensions.Configuration;

namespace IntelliTrader.Signals.Base
{
    public class SignalDefinition : ISignalDefinition
    {
        public string Name { get; set; }
        public string Receiver { get; set; }
        public IConfigurationSection Configuration { get; set; }
    }
}
