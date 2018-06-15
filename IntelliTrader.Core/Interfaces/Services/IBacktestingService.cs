using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IBacktestingService : IConfigurableService
    {
        IBacktestingConfig Config { get; }
        object SyncRoot { get; }
        void Start();
        void Stop();
        void Complete(int skippedSignalSnapshots, int skippedTickerSnapshots);
        string GetSnapshotFilePath(string snapshotEntity);
        Dictionary<string, IEnumerable<ISignal>> GetCurrentSignals();
        Dictionary<string, ITicker> GetCurrentTickers();
        int GetTotalSnapshots();
    }
}
