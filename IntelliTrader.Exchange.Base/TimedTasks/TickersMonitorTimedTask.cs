using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;

namespace IntelliTrader.Exchange
{
    internal class TickersMonitorTimedTask : HighResolutionTimedTask
    {
        private readonly ILoggingService loggingService;
        private readonly IExchangeService exchangeService;

        public TickersMonitorTimedTask(ILoggingService loggingService, IExchangeService exchangeService)
        {
            this.loggingService = loggingService;
            this.exchangeService = exchangeService;
        }

        protected override void Run()
        {
            if (exchangeService.GetTimeElapsedSinceLastTickersUpdate().TotalSeconds > ExchangeService.MAX_TICKERS_AGE_TO_RECONNECT_SECONDS)
            {
                loggingService.Info("Exchange max tickers age reached, reconnecting...");
                exchangeService.DisconnectTickersWebsocket();
                exchangeService.ConnectTickersWebsocket();
            }
        }
    }
}
