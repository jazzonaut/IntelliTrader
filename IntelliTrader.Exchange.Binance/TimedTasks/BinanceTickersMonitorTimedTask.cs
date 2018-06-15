using IntelliTrader.Core;

namespace IntelliTrader.Exchange.Binance
{
    internal class BinanceTickersMonitorTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly BinanceExchangeService binanceExchangeService;

        public BinanceTickersMonitorTimedTask(ILoggingService loggingService, BinanceExchangeService binanceExchangeService)
        {
            this.loggingService = loggingService;
            this.binanceExchangeService = binanceExchangeService;
        }

        public override void Run()
        {
            if (binanceExchangeService.GetTimeElapsedSinceLastTickersUpdate().TotalSeconds > BinanceExchangeService.MAX_TICKERS_AGE_TO_RECONNECT_SECONDS)
            {
                loggingService.Info("Binance Exchange max tickers age reached, reconnecting...");
                binanceExchangeService.DisconnectTickersWebsocket();
                binanceExchangeService.ConnectTickersWebsocket();
            }
        }
    }
}
