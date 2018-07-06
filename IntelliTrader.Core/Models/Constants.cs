using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public static class Constants
    {
        public static class ServiceNames
        {
            public const string CoreService = "Core";
            public const string CachingService = "Caching";
            public const string LoggingService = "Logging";
            public const string TradingService = "Trading";
            public const string ExchangeService = "Exchange";
            public const string SignalsService = "Signals";
            public const string RulesService = "Rules";
            public const string NotificationService = "Notification";
            public const string WebService = "Web";
            public const string BacktestingService = "Backtesting";
            public const string BacktestingExchangeService = "BacktestingExchange";
            public const string BacktestingSignalsService = "BacktestingSignals";
        }

        public static class HealthChecks
        {
            public const string AccountRefreshed = "Account refreshed";
            public const string TickersUpdated = "Tickers updated";
            public const string TradingPairsProcessed = "Trading pairs processed";
            public const string TradingViewCryptoSignalsReceived = "TV Signals received";
            public const string SignalRulesProcessed = "Signals rules processed";
            public const string TradingRulesProcessed = "Trading rules processed";
            public const string BacktestingSignalsSnapshotTaken = "Backtesting signals snapshot taken";
            public const string BacktestingTickersSnapshotTaken = "Backtesting tickers snapshot taken";
            public const string BacktestingSignalsSnapshotLoaded = "Backtesting signals snapshot loaded";
            public const string BacktestingTickersSnapshotLoaded = "Backtesting tickers snapshot loaded";
        }

        public static class SignalRuleActions
        {
            public const string Swap = "Swap";
        }

        public static class SnapshotEntities
        {
            public const string Signals = "signals";
            public const string Tickers = "tickers";
        }

        public static class TaskDelays
        {
            public const int ZeroDelay = 0;
            public const int LowDelay = 1200;
            public const int MidDelay = 2400;
            public const int NormalDelay = 3300;
            public const int HighDelay = 4700;
        }

        public static class Markets
        {
            public const string BTC = "BTC";
            public const string ETH = "ETH";
            public const string BNB = "BNB";
            public const string USDT = "USDT";
        }
    }
}
