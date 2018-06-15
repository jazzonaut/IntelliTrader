using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IBacktestingConfig
    {
        bool Enabled { get; }
        bool Replay { get; }
        bool ReplayOutput { get; }
        double ReplaySpeed { get; }
        int? ReplayStartIndex { get; }
        int? ReplayEndIndex { get; }
        bool DeleteLogs { get; }
        bool DeleteAccountData { get; }
        string CopyAccountDataPath { get; }
        int SnapshotsInterval { get; }
        string SnapshotsPath { get; }
    }
}
