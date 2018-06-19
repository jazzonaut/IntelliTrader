using System;
using System.Collections.Concurrent;

namespace IntelliTrader.Core
{
    public interface ICoreService : IConfigurableService
    {
        ICoreConfig Config { get; }
        string Version { get; }
        void Start();
        void Stop();
        void Restart();
    }
}
