using IntelliTrader.Core;
using System.IO;
using System.Linq;
using ZeroFormatter;

namespace IntelliTrader.Backtesting
{
    internal class BacktestingSaveSnapshotsTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;
        private readonly ISignalsService signalsService;
        private readonly IBacktestingService backtestingService;

        public BacktestingSaveSnapshotsTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService, ITradingService tradingService, ISignalsService signalsService, IBacktestingService backtestingService)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
            this.signalsService = signalsService;
            this.backtestingService = backtestingService;
        }

        protected override void Run()
        {
            if (backtestingService.Config.Enabled && !backtestingService.Config.Replay)
            {
                TakeSignalsSnapshot();
                TakeTickersSnapshot();
            }
        }

        private void TakeSignalsSnapshot()
        {
            var signals = signalsService.GetAllSignals().Select(s => SignalData.FromSignal(s));

            byte[] signalBytes = ZeroFormatterSerializer.Serialize(signals);
            string signalsSnapshotFilePath = backtestingService.GetSnapshotFilePath(Constants.SnapshotEntities.Signals);
            var signalsSnapshotFile = new FileInfo(signalsSnapshotFilePath);
            signalsSnapshotFile.Directory.Create();
            File.WriteAllBytes(signalsSnapshotFilePath, signalBytes);

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.BacktestingSignalsSnapshotTaken, $"Signals: {signals.Count()}");
        }

        private void TakeTickersSnapshot()
        {
            var tickers = tradingService.GetTickers().Select(t => TickerData.FromTicker(t));

            byte[] tickerBytes = ZeroFormatterSerializer.Serialize(tickers);
            string tickersSnapshotFilePath = backtestingService.GetSnapshotFilePath(Constants.SnapshotEntities.Tickers);
            var tickersSnapshotFile = new FileInfo(tickersSnapshotFilePath);
            tickersSnapshotFile.Directory.Create();
            File.WriteAllBytes(tickersSnapshotFilePath, tickerBytes);

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.BacktestingTickersSnapshotTaken, $"Tickers: {tickers.Count()}");
        }
    }
}
