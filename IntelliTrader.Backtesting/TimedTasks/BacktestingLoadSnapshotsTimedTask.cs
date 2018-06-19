using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ZeroFormatter;

namespace IntelliTrader.Backtesting
{
    internal class BacktestingLoadSnapshotsTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;
        private readonly IBacktestingService backtestingService;

        private Queue<string> allSignalSnapshotPaths;
        private Queue<string> allTickerSnapshotPaths;
        private Dictionary<string, IEnumerable<ISignal>> currentSignals;
        private Dictionary<string, ITicker> currentTickers;
        private Stopwatch backtestingTimer;

        private int totalSignalSnapshots;
        private int totalTickerSnapshots;
        public int loadedSignalSnapshots;
        public int loadedTickerSnapshots;
        private bool isCompleted;

        public object Config { get; }

        public BacktestingLoadSnapshotsTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService, IBacktestingService backtestingService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
            this.backtestingService = backtestingService;

            PopulateSnapshotPaths();
        }

        protected override void Run()
        {
            if (!isCompleted)
            {
                LoadNextSnapshots();
            }
        }

        public Dictionary<string, IEnumerable<ISignal>> GetCurrentSignals()
        {
            lock (backtestingService.SyncRoot)
            {
                return currentSignals;
            }
        }

        public Dictionary<string, ITicker> GetCurrentTickers()
        {
            lock (backtestingService.SyncRoot)
            {
                return currentTickers;
            }
        }

        public int GetTotalSnapshots()
        {
            return totalSignalSnapshots;
        }

        private void LoadNextSnapshots()
        {
            lock (backtestingService.SyncRoot)
            {
                if (loadedSignalSnapshots == 0 && loadedTickerSnapshots == 0)
                {
                    loggingService.Info($"<<<--- Backtesting started. Total signals snapshots: {totalSignalSnapshots}, Total tickers snapshots: {totalTickerSnapshots} --->>>");
                    backtestingTimer = Stopwatch.StartNew();
                }

                if (allSignalSnapshotPaths.TryDequeue(out string currentSignalsSnapshotPath))
                {
                    try
                    {
                        byte[] currentSignalsSnapshotBytes = File.ReadAllBytes(currentSignalsSnapshotPath);
                        IEnumerable<ISignal> data = ZeroFormatterSerializer.Deserialize<IEnumerable<SignalData>>(currentSignalsSnapshotBytes).Select(s => s.ToSignal()).ToList();
                        currentSignals = data.GroupBy(s => s.Pair).ToDictionary(s => s.Key, s => s.AsEnumerable());
                        loadedSignalSnapshots++;

                        var currentSignalsSnapshotFile = currentSignalsSnapshotPath.Substring(currentSignalsSnapshotPath.Length - 27);
                        currentSignalsSnapshotFile = currentSignalsSnapshotFile.Replace('\\', '-').Replace('/', '-');
                        if (backtestingService.Config.ReplayOutput && loadedSignalSnapshots % 100 == 0)
                        {
                            loggingService.Info($"<<<--- ({loadedSignalSnapshots}/{totalSignalSnapshots}) Load signals snapshot file: {currentSignalsSnapshotFile} --->>>");
                        }
                        healthCheckService.UpdateHealthCheck(Constants.HealthChecks.BacktestingSignalsSnapshotLoaded, $"File: {currentSignalsSnapshotFile}");
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error($"<<<--- Unable to load signals snapshot file: {currentSignalsSnapshotPath} --->>>", ex);
                    }
                }

                if (allTickerSnapshotPaths.TryDequeue(out string currentTickersSnapshotPath))
                {
                    try
                    {
                        byte[] currentTickersSnapshotBytes = File.ReadAllBytes(currentTickersSnapshotPath);
                        IEnumerable<ITicker> data = ZeroFormatterSerializer.Deserialize<IEnumerable<TickerData>>(currentTickersSnapshotBytes).Select(t => t.ToTicker()).ToList();
                        currentTickers = data.ToDictionary(t => t.Pair, t => t);
                        loadedTickerSnapshots++;

                        var currentTickersSnapshotFile = currentTickersSnapshotPath.Substring(currentTickersSnapshotPath.Length - 27);
                        currentTickersSnapshotFile = currentTickersSnapshotFile.Replace('\\', '-').Replace('/', '-');
                        if (backtestingService.Config.ReplayOutput && loadedTickerSnapshots % 100 == 0)
                        {
                            loggingService.Info($"<<<--- ({loadedTickerSnapshots}/{totalTickerSnapshots}) Load tickers snapshot file: {currentTickersSnapshotFile} --->>>");
                        }
                        healthCheckService.UpdateHealthCheck(Constants.HealthChecks.BacktestingTickersSnapshotLoaded, $"File: {currentTickersSnapshotFile}");
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error($"<<<--- Unable to load tickers snapshot file: {currentTickersSnapshotPath} --->>>", ex);
                    }
                }

                if (currentSignalsSnapshotPath == null && currentTickersSnapshotPath == null)
                {
                    isCompleted = true;
                    backtestingTimer.Stop();
                    loggingService.Info($"<<<--- Backtesting finished in {Math.Round(backtestingTimer.Elapsed.TotalSeconds)} seconds --->>>");
                    backtestingService.Complete(totalSignalSnapshots - loadedSignalSnapshots, totalTickerSnapshots - loadedTickerSnapshots);
                }
            }
        }

        private void PopulateSnapshotPaths()
        {
            var signalsSnapshotPath = Path.Combine(Directory.GetCurrentDirectory(), backtestingService.Config.SnapshotsPath, Constants.SnapshotEntities.Signals);
            if (Directory.Exists(signalsSnapshotPath))
            {
                var files = Directory.EnumerateFiles(signalsSnapshotPath, "*." + BacktestingService.SNAPSHOT_FILE_EXTENSION, SearchOption.AllDirectories);
                allSignalSnapshotPaths = new Queue<string>(files.Take(backtestingService.Config.ReplayEndIndex ?? files.Count()).Skip(backtestingService.Config.ReplayStartIndex ?? 0));
            }
            else
            {
                allSignalSnapshotPaths = new Queue<string>();
            }
            totalSignalSnapshots = allSignalSnapshotPaths.Count;

            var tickersSnapshotPath = Path.Combine(Directory.GetCurrentDirectory(), backtestingService.Config.SnapshotsPath, Constants.SnapshotEntities.Tickers);
            if (Directory.Exists(tickersSnapshotPath))
            {
                var files = Directory.EnumerateFiles(tickersSnapshotPath, "*." + BacktestingService.SNAPSHOT_FILE_EXTENSION, SearchOption.AllDirectories);
                allTickerSnapshotPaths = new Queue<string>(files.Take(backtestingService.Config.ReplayEndIndex ?? files.Count()).Skip(backtestingService.Config.ReplayStartIndex ?? 0));
            }
            else
            {
                allTickerSnapshotPaths = new Queue<string>();
            }
            totalTickerSnapshots = allTickerSnapshotPaths.Count;
        }
    }
}
