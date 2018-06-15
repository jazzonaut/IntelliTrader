using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IntelliTrader.Trading
{
    internal class TradingService : ConfigrableServiceBase<TradingConfig>, ITradingService
    {
        private const int MIN_INTERVAL_BETWEEN_BUY_AND_SELL = 10000;

        public override string ServiceName => Constants.ServiceNames.TradingService;

        ITradingConfig ITradingService.Config => Config;

        public object SyncRoot { get; private set; } = new object();

        public IModuleRules Rules { get; private set; }
        public TradingRulesConfig RulesConfig { get; private set; }

        public ITradingAccount Account { get; private set; }
        public ConcurrentStack<IOrderDetails> OrderHistory { get; private set; } = new ConcurrentStack<IOrderDetails>();
        public bool IsTradingSuspended { get; private set; }

        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly IRulesService rulesService;
        private readonly IExchangeService exchangeService;
        private ISignalsService signalsService;

        private TradingTimedTask tradingTimedTask;
        private TradingRulesTimedTask tradingRulesTimedTask;
        private AccountTimedTask accountTimedTask;

        private bool isReplayingSnapshots;
        private bool tradingForcefullySuspended;

        public TradingService(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, IRulesService rulesService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.rulesService = rulesService;
            this.isReplayingSnapshots = Application.Resolve<IBacktestingService>().Config.Enabled && Application.Resolve<IBacktestingService>().Config.Replay;

            if (isReplayingSnapshots)
            {
                this.exchangeService = Application.ResolveOptionalNamed<IExchangeService>(Constants.ServiceNames.BacktestingExchangeService);
            }
            else
            {
                this.exchangeService = Application.ResolveOptionalNamed<IExchangeService>(Config.Exchange);
            }

            if (this.exchangeService == null)
            {
                throw new Exception($"Unsupported exchange: {Config.Exchange}");
            }
        }

        public void Start()
        {
            loggingService.Info($"Start Trading service (Virtual: {Config.VirtualTrading})...");

            IsTradingSuspended = true;

            OnTradingRulesChanged();
            rulesService.RegisterRulesChangeCallback(OnTradingRulesChanged);

            exchangeService.Start(Config.VirtualTrading);

            signalsService = Application.Resolve<ISignalsService>();

            if (!Config.VirtualTrading)
            {
                Account = new ExchangeAccount(loggingService, notificationService, healthCheckService, signalsService, this);
            }
            else
            {
                Account = new VirtualAccount(loggingService, notificationService, healthCheckService, signalsService, this);
            }

            accountTimedTask = new AccountTimedTask(loggingService, healthCheckService, this);
            accountTimedTask.RunInterval = (float)(Config.AccountRefreshInterval * 1000 / Application.Speed);
            accountTimedTask.Run();
            Application.Resolve<ICoreService>().AddTask(nameof(AccountTimedTask), accountTimedTask);

            if (signalsService.Config.Enabled)
            {
                signalsService.Start();
            }

            tradingTimedTask = new TradingTimedTask(loggingService, notificationService, healthCheckService, signalsService, this);
            tradingTimedTask.RunInterval = (float)(Config.TradingCheckInterval * 1000 / Application.Speed);
            tradingTimedTask.StartDelay = Constants.TimedTasks.StandardDelay / Application.Speed;
            tradingTimedTask.LoggingEnabled = !isReplayingSnapshots;
            Application.Resolve<ICoreService>().AddTask(nameof(TradingTimedTask), tradingTimedTask);

            tradingRulesTimedTask = new TradingRulesTimedTask(loggingService, notificationService, healthCheckService, rulesService, signalsService, this);
            tradingRulesTimedTask.RunInterval = (float)(RulesConfig.CheckInterval * 1000 / Application.Speed);
            Application.Resolve<ICoreService>().AddTask(nameof(TradingRulesTimedTask), tradingRulesTimedTask);

            IsTradingSuspended = false;

            loggingService.Info("Trading service started");
        }

        public void Stop()
        {
            loggingService.Info("Stop Trading service...");

            exchangeService.Stop();

            if (signalsService.Config.Enabled)
            {
                signalsService.Stop();
            }

            Application.Resolve<ICoreService>().StopTask(nameof(TradingTimedTask));
            Application.Resolve<ICoreService>().RemoveTask(nameof(TradingTimedTask));

            Application.Resolve<ICoreService>().StopTask(nameof(TradingRulesTimedTask));
            Application.Resolve<ICoreService>().RemoveTask(nameof(TradingRulesTimedTask));

            Application.Resolve<ICoreService>().StopTask(nameof(AccountTimedTask));
            Application.Resolve<ICoreService>().RemoveTask(nameof(AccountTimedTask));

            Account.Dispose();

            rulesService.UnregisterRulesChangeCallback(OnTradingRulesChanged);

            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingRulesProcessed);
            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingPairsProcessed);

            loggingService.Info("Trading service stopped");
        }

        public void ResumeTrading(bool forced)
        {
            if (IsTradingSuspended && (!tradingForcefullySuspended || forced))
            {
                loggingService.Info("Trading started");
                IsTradingSuspended = false;

                Application.Resolve<ICoreService>().StartTask(nameof(TradingTimedTask));
                Application.Resolve<ICoreService>().StartTask(nameof(TradingRulesTimedTask));
                tradingRulesTimedTask.Run();
            }
        }

        public void SuspendTrading(bool forced)
        {
            if (!IsTradingSuspended)
            {
                loggingService.Info("Trading suspended");
                IsTradingSuspended = true;
                tradingForcefullySuspended = forced;

                Application.Resolve<ICoreService>().StopTask(nameof(TradingTimedTask));
                Application.Resolve<ICoreService>().StopTask(nameof(TradingRulesTimedTask));
                tradingTimedTask.ClearTrailing();
            }
        }

        public IPairConfig GetPairConfig(string pair)
        {
            return tradingRulesTimedTask.GetPairConfig(pair);
        }

        public void ReapplyTradingRules()
        {
            tradingRulesTimedTask.Run();
        }

        public void Buy(BuyOptions options)
        {
            lock (SyncRoot)
            {
                IRule rule = signalsService.Rules.Entries.FirstOrDefault(r => r.Name == options.Metadata.SignalRule);

                ITradingPair swappedPair = Account.GetTradingPairs().OrderBy(p => p.CurrentMargin).FirstOrDefault(tradingPair =>
                {
                    IPairConfig pairConfig = GetPairConfig(tradingPair.Pair);
                    return pairConfig.SellEnabled && pairConfig.SwapEnabled && pairConfig.SwapSignalRules != null && pairConfig.SwapSignalRules.Contains(options.Metadata.SignalRule) &&
                           pairConfig.SwapTimeout < (DateTimeOffset.Now - tradingPair.OrderDates.Max()).TotalSeconds;
                });

                if (swappedPair != null)
                {
                    Swap(new SwapOptions(swappedPair.Pair, options.Pair, options.Metadata));
                }
                else if (rule?.Action != Constants.SignalRuleActions.Swap)
                {
                    if (CanBuy(options, out string message))
                    {
                        tradingTimedTask.InitiateBuy(options);
                    }
                    else
                    {
                        loggingService.Debug(message);
                    }
                }
            }
        }

        public void Sell(SellOptions options)
        {
            lock (SyncRoot)
            {
                if (CanSell(options, out string message))
                {
                    tradingTimedTask.InitiateSell(options);
                }
                else
                {
                    loggingService.Debug(message);
                }
            }
        }

        public void Swap(SwapOptions options)
        {
            lock (SyncRoot)
            {
                if (CanSwap(options, out string message))
                {
                    ITradingPair oldTradingPair = Account.GetTradingPair(options.OldPair);

                    var sellOptions = new SellOptions(options.OldPair)
                    {
                        Swap = true,
                        SwapPair = options.NewPair,
                        ManualOrder = options.ManualOrder
                    };

                    if (CanSell(sellOptions, out message))
                    {
                        decimal currentMargin = oldTradingPair.CurrentMargin;
                        decimal additionalCosts = oldTradingPair.AverageCostPaid - oldTradingPair.CurrentCost + (oldTradingPair.Metadata.AdditionalCosts ?? 0);
                        int additionalDCALevels = oldTradingPair.DCALevel;

                        IOrderDetails sellOrderDetails = tradingTimedTask.PlaceSellOrder(sellOptions);
                        if (!Account.HasTradingPair(options.OldPair))
                        {
                            var buyOptions = new BuyOptions(options.NewPair)
                            {
                                Swap = true,
                                ManualOrder = options.ManualOrder,
                                MaxCost = sellOrderDetails.AverageCost,
                                Metadata = options.Metadata
                            };
                            buyOptions.Metadata.LastBuyMargin = currentMargin;
                            buyOptions.Metadata.SwapPair = options.OldPair;
                            buyOptions.Metadata.AdditionalDCALevels = additionalDCALevels;
                            buyOptions.Metadata.AdditionalCosts = additionalCosts;
                            IOrderDetails buyOrderDetails = tradingTimedTask.PlaceBuyOrder(buyOptions);

                            var newTradingPair = Account.GetTradingPair(options.NewPair) as TradingPair;
                            if (newTradingPair != null)
                            {
                                if (sellOrderDetails.Fees != 0 && sellOrderDetails.FeesCurrency != null)
                                {
                                    if (sellOrderDetails.FeesCurrency == Config.Market)
                                    {
                                        newTradingPair.Metadata.AdditionalCosts += sellOrderDetails.Fees;
                                    }
                                    else
                                    {
                                        string feesPair = sellOrderDetails.FeesCurrency + Config.Market;
                                        newTradingPair.Metadata.AdditionalCosts += GetCurrentPrice(feesPair) * sellOrderDetails.Fees;
                                    }
                                }
                                loggingService.Info($"Swap {oldTradingPair.FormattedName} for {newTradingPair.FormattedName}. Old margin: {oldTradingPair.CurrentMargin:0.00}, new margin: {newTradingPair.CurrentMargin:0.00}");
                            }
                            else
                            {
                                loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}. Reason: failed to buy {options.NewPair}");
                                notificationService.Notify($"Unable to swap {options.OldPair} for {options.NewPair}: Failed to buy {options.NewPair}");
                            }
                        }
                        else
                        {
                            loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}. Reason: failed to sell {options.OldPair}");
                        }
                    }
                    else
                    {
                        loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}: {message}");
                    }
                }
                else
                {
                    loggingService.Info(message);
                }
            }
        }

        public bool CanBuy(BuyOptions options, out string message)
        {
            IPairConfig pairConfig = GetPairConfig(options.Pair);

            if (!options.ManualOrder && !options.Swap && IsTradingSuspended)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: trading suspended";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && !pairConfig.BuyEnabled)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: buying not enabled";
                return false;
            }
            else if (!options.ManualOrder && Config.ExcludedPairs.Contains(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: exluded pair";
                return false;
            }
            else if (!options.ManualOrder && !options.IgnoreExisting && Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: pair already exists";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && Config.MaxPairs != 0 && Account.GetTradingPairs().Count() >= Config.MaxPairs && !Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: maximum pairs reached";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && pairConfig.BuyMinBalance != 0 && (Account.GetBalance() - options.MaxCost) < pairConfig.BuyMinBalance)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: minimum balance reached";
                return false;
            }
            else if (GetCurrentPrice(options.Pair) <= 0)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: invalid price";
                return false;
            }
            else if (Account.GetBalance() < options.MaxCost)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: not enough balance";
                return false;
            }
            else if (options.Amount == null && options.MaxCost == null || options.Amount != null && options.MaxCost != null)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: either max cost or amount needs to be specified (not both)";
            }
            else if (!options.ManualOrder && !options.Swap && pairConfig.BuySamePairTimeout > 0 && OrderHistory.Any(h => h.Side == OrderSide.Buy && h.Pair == options.Pair) &&
                (DateTimeOffset.Now - OrderHistory.Where(h => h.Pair == options.Pair).Max(h => h.Date)).TotalSeconds < pairConfig.BuySamePairTimeout)
            {
                var elapsedSeconds = (DateTimeOffset.Now - OrderHistory.Where(h => h.Pair == options.Pair).Max(h => h.Date)).TotalSeconds;
                message = $"Cancel buy request for {options.Pair}. Reason: buy same pair timeout (elapsed: {elapsedSeconds:0.#}, timeout: {pairConfig.BuySamePairTimeout:0.#})";
                return false;
            }

            message = null;
            return true;
        }

        public bool CanSell(SellOptions options, out string message)
        {
            IPairConfig pairConfig = GetPairConfig(options.Pair);

            if (!options.ManualOrder && IsTradingSuspended)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: trading suspended";
                return false;
            }
            else if (!options.ManualOrder && !pairConfig.SellEnabled)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: selling not enabled";
                return false;
            }
            else if (!options.ManualOrder && Config.ExcludedPairs.Contains(options.Pair))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: excluded pair";
                return false;
            }
            else if (!Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: pair does not exist";
                return false;
            }
            else if ((DateTimeOffset.Now - Account.GetTradingPair(options.Pair).OrderDates.Max()).TotalMilliseconds < (MIN_INTERVAL_BETWEEN_BUY_AND_SELL / Application.Speed))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: pair just bought";
                return false;
            }
            message = null;
            return true;
        }

        public bool CanSwap(SwapOptions options, out string message)
        {
            if (!Account.HasTradingPair(options.OldPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: pair does not exist";
                return false;
            }
            else if (Account.HasTradingPair(options.NewPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: pair already exists";
                return false;
            }
            else if (!options.ManualOrder && !GetPairConfig(options.OldPair).SellEnabled)
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: selling not enabled";
                return false;
            }
            else if (!options.ManualOrder && !GetPairConfig(options.NewPair).BuyEnabled)
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: buying not enabled";
                return false;
            }
            else if (!GetMarketPairs().Contains(options.NewPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: {options.NewPair} is not a valid pair";
                return false;
            }

            message = null;
            return true;
        }

        public void LogOrder(IOrderDetails order)
        {
            OrderHistory.Push(order);
        }

        public List<string> GetTrailingBuys()
        {
            return tradingTimedTask.GetTrailingBuys();
        }

        public List<string> GetTrailingSells()
        {
            return tradingTimedTask.GetTrailingSells();
        }

        public IEnumerable<ITicker> GetTickers()
        {
            return exchangeService.GetTickers(Config.Market).Result;
        }

        public IEnumerable<string> GetMarketPairs()
        {
            return exchangeService.GetMarketPairs(Config.Market).Result;
        }

        public Dictionary<string, decimal> GetAvailableAmounts()
        {
            return exchangeService.GetAvailableAmounts().Result;
        }

        public IEnumerable<IOrderDetails> GetMyTrades(string pair)
        {
            return exchangeService.GetMyTrades(pair).Result;
        }

        public IOrderDetails PlaceOrder(IOrder order)
        {
            return exchangeService.PlaceOrder(order).Result;
        }

        public decimal GetCurrentPrice(string pair)
        {
            return exchangeService.GetLastPrice(pair).Result;
        }

        private void OnTradingRulesChanged()
        {
            Rules = rulesService.GetRules(ServiceName);
            RulesConfig = Rules.GetConfiguration<TradingRulesConfig>();
        }

        protected override void PrepareConfig()
        {
            if (Config.ExcludedPairs == null)
            {
                Config.ExcludedPairs = new List<string>();
            }

            if (Config.DCALevels == null)
            {
                Config.DCALevels = new List<DCALevel>();
            }
        }
    }
}
