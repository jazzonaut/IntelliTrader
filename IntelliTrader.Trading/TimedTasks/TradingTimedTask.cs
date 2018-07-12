using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Trading
{
    public class TradingTimedTask : HighResolutionTimedTask
    {
        public bool LoggingEnabled { get; set; } = true;

        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ISignalsService signalsService;
        private readonly ITradingService tradingService;
        private readonly IOrderingService orderingService;

        private readonly ConcurrentDictionary<string, BuyTrailingInfo> trailingBuys = new ConcurrentDictionary<string, BuyTrailingInfo>();
        private readonly ConcurrentDictionary<string, SellTrailingInfo> trailingSells = new ConcurrentDictionary<string, SellTrailingInfo>();

        public TradingTimedTask(ILoggingService loggingService, INotificationService notificationService, 
            IHealthCheckService healthCheckService, ISignalsService signalsService, IOrderingService orderingService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.signalsService = signalsService;
            this.orderingService = orderingService;
            this.tradingService = tradingService;
        }

        protected override void Run()
        {
            lock (tradingService.SyncRoot)
            {
                ProcessTradingPairs();
            }
        }

        public void InitiateBuy(BuyOptions options)
        {
            IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
            if (!options.ManualOrder && pairConfig.BuyTrailing != 0)
            {
                if (!trailingBuys.ContainsKey(options.Pair))
                {
                    StopTrailingSell(options.Pair);
                    decimal currentPrice = tradingService.GetPrice(options.Pair);
                    decimal currentMargin = 0;

                    var trailingInfo = new BuyTrailingInfo
                    {
                        BuyOptions = options,
                        Trailing = pairConfig.BuyTrailing,
                        TrailingStopMargin = pairConfig.BuyTrailingStopMargin,
                        TrailingStopAction = pairConfig.BuyTrailingStopAction,
                        InitialPrice = currentPrice,
                        LastTrailingMargin = currentMargin,
                        BestTrailingMargin = currentMargin
                    };

                    if (trailingBuys.TryAdd(options.Pair, trailingInfo))
                    {
                        if (LoggingEnabled)
                        {
                            ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                            loggingService.Info($"Start trailing buy {tradingPair?.FormattedName ?? options.Pair}. " +
                                $"Price: {currentPrice:0.00000000}, Margin: {currentMargin:0.00}");
                        }
                    }
                }
            }
            else
            {
                orderingService.PlaceBuyOrder(options);
            }
        }

        public void InitiateSell(SellOptions options)
        {
            IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
            if (!options.ManualOrder && pairConfig.SellTrailing != 0)
            {
                if (!trailingSells.ContainsKey(options.Pair))
                {
                    StopTrailingBuy(options.Pair);
                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                    tradingPair.SetCurrentValues(tradingService.GetPrice(tradingService.NormalizePair(options.Pair)), tradingService.Exchange.GetPriceSpread(options.Pair));

                    var trailingInfo = new SellTrailingInfo
                    {
                        SellOptions = options,
                        SellMargin = pairConfig.SellMargin,
                        Trailing = pairConfig.SellTrailing,
                        TrailingStopMargin = pairConfig.SellTrailingStopMargin,
                        TrailingStopAction = pairConfig.SellTrailingStopAction,
                        InitialPrice = tradingPair.CurrentPrice,
                        LastTrailingMargin = tradingPair.CurrentMargin,
                        BestTrailingMargin = tradingPair.CurrentMargin
                    };

                    if (trailingSells.TryAdd(options.Pair, trailingInfo))
                    {
                        if (LoggingEnabled)
                        {
                            loggingService.Info($"Start trailing sell {tradingPair.FormattedName}. " +
                                $"Price: {tradingPair.CurrentPrice:0.00000000}, Margin: {tradingPair.CurrentMargin:0.00}");
                        }
                    }
                }
            }
            else
            {
                orderingService.PlaceSellOrder(options);
            }
        }

        public void ProcessTradingPairs()
        {
            int traidingPairsCount = 0;

            foreach (var tradingPair in tradingService.Account.GetTradingPairs())
            {
                IPairConfig pairConfig = tradingService.GetPairConfig(tradingPair.Pair);
                tradingPair.SetCurrentValues(tradingService.GetPrice(tradingService.NormalizePair(tradingPair.Pair)), tradingService.Exchange.GetPriceSpread(tradingPair.Pair));
                tradingPair.Metadata.TradingRules = pairConfig.Rules.ToList();
                tradingPair.Metadata.CurrentRating = tradingPair.Metadata.Signals != null ? signalsService.GetRating(tradingPair.Pair, tradingPair.Metadata.Signals) : null;
                tradingPair.Metadata.CurrentGlobalRating = signalsService.GetGlobalRating();

                if (trailingSells.TryGetValue(tradingPair.Pair, out SellTrailingInfo sellTrailingInfo))
                {
                    if (pairConfig.SellEnabled)
                    {
                        if (Math.Round(tradingPair.CurrentMargin, 1) != Math.Round(sellTrailingInfo.LastTrailingMargin, 1))
                        {
                            if (LoggingEnabled)
                            {
                                loggingService.Info($"Continue trailing sell {tradingPair.FormattedName}. " +
                                    $"Price: {tradingPair.CurrentPrice:0.00000000}, Margin: {tradingPair.CurrentMargin:0.00}");
                            }
                        }

                        if (tradingPair.CurrentMargin <= sellTrailingInfo.TrailingStopMargin || tradingPair.CurrentMargin < 
                            (sellTrailingInfo.BestTrailingMargin - sellTrailingInfo.Trailing))
                        {
                            StopTrailingSell(tradingPair.Pair);

                            if (tradingPair.CurrentMargin > 0 || sellTrailingInfo.SellMargin < 0)
                            {
                                if (sellTrailingInfo.TrailingStopAction == SellTrailingStopAction.Sell || tradingPair.CurrentMargin > sellTrailingInfo.TrailingStopMargin)
                                {
                                    orderingService.PlaceSellOrder(sellTrailingInfo.SellOptions);
                                }
                                else
                                {
                                    if (LoggingEnabled)
                                    {
                                        loggingService.Info($"Stop trailing sell {tradingPair.FormattedName}. Reason: stop margin reached");
                                    }
                                }
                            }
                            else
                            {
                                if (LoggingEnabled)
                                {
                                    loggingService.Info($"Stop trailing sell {tradingPair.FormattedName}. Reason: negative margin");
                                }
                            }
                        }
                        else
                        {
                            sellTrailingInfo.LastTrailingMargin = tradingPair.CurrentMargin;
                            if (tradingPair.CurrentMargin > sellTrailingInfo.BestTrailingMargin)
                            {
                                sellTrailingInfo.BestTrailingMargin = tradingPair.CurrentMargin;
                            }
                        }
                    }
                    else
                    {
                        StopTrailingSell(tradingPair.Pair);
                    }
                }
                else
                {
                    if (pairConfig.SellEnabled && tradingPair.CurrentMargin >= pairConfig.SellMargin)
                    {
                        InitiateSell(new SellOptions(tradingPair.Pair));
                    }
                    else if (pairConfig.SellEnabled && pairConfig.SellStopLossEnabled && 
                        tradingPair.CurrentMargin <= pairConfig.SellStopLossMargin && 
                        tradingPair.CurrentAge >= pairConfig.SellStopLossMinAge &&
                        (pairConfig.NextDCAMargin == null || !pairConfig.SellStopLossAfterDCA))
                    {
                        if (LoggingEnabled)
                        {
                            loggingService.Info($"Stop loss triggered for {tradingPair.FormattedName}. Margin: {tradingPair.CurrentMargin:0.00}");
                        }
                        orderingService.PlaceSellOrder(new SellOptions(tradingPair.Pair));
                    }
                    else if (pairConfig.NextDCAMargin != null && pairConfig.BuyEnabled && pairConfig.NextDCAMargin != null &&
                        !trailingBuys.ContainsKey(tradingPair.Pair) && !trailingSells.ContainsKey(tradingPair.Pair))
                    {
                        if (tradingPair.CurrentMargin <= pairConfig.NextDCAMargin)
                        {
                            var buyOptions = new BuyOptions(tradingPair.Pair)
                            {
                                MaxCost = tradingPair.ActualCost * pairConfig.BuyMultiplier,
                                IgnoreExisting = true
                            };

                            if (tradingService.CanBuy(buyOptions, message: out string message))
                            {
                                if (LoggingEnabled)
                                {
                                    loggingService.Info($"DCA triggered for {tradingPair.FormattedName}. Margin: {tradingPair.CurrentMargin:0.00}, " +
                                        $"Level: {pairConfig.NextDCAMargin:0.00}, Multiplier: {pairConfig.BuyMultiplier}");
                                }
                                InitiateBuy(buyOptions);
                            }
                        }
                    }
                }

                traidingPairsCount++;
            }

            foreach (var kvp in trailingBuys)
            {
                string pair = kvp.Key;
                BuyTrailingInfo buyTrailingInfo = kvp.Value;
                ITradingPair tradingPair = tradingService.Account.GetTradingPair(pair);
                IPairConfig pairConfig = tradingService.GetPairConfig(pair);
                decimal currentPrice = tradingService.GetPrice(pair);
                decimal currentMargin = Utils.CalculatePercentage(buyTrailingInfo.InitialPrice, currentPrice);

                if (pairConfig.BuyEnabled)
                {
                    if (Math.Round(currentMargin, 1) != Math.Round(buyTrailingInfo.LastTrailingMargin, 1))
                    {
                        if (LoggingEnabled)
                        {
                            loggingService.Info($"Continue trailing buy {tradingPair?.FormattedName ?? pair}. Price: {currentPrice:0.00000000}, Margin: {currentMargin:0.00}");
                        }
                    }

                    if (currentMargin >= buyTrailingInfo.TrailingStopMargin || currentMargin > (buyTrailingInfo.BestTrailingMargin - buyTrailingInfo.Trailing))
                    {
                        StopTrailingBuy(pair);

                        if (buyTrailingInfo.TrailingStopAction == BuyTrailingStopAction.Buy || currentMargin < buyTrailingInfo.TrailingStopMargin)
                        {
                            orderingService.PlaceBuyOrder(buyTrailingInfo.BuyOptions);
                        }
                        else
                        {
                            if (LoggingEnabled)
                            {
                                loggingService.Info($"Stop trailing buy {tradingPair?.FormattedName ?? pair}. Reason: stop margin reached");
                            }
                        }
                    }
                    else
                    {
                        buyTrailingInfo.LastTrailingMargin = currentMargin;
                        if (currentMargin < buyTrailingInfo.BestTrailingMargin)
                        {
                            buyTrailingInfo.BestTrailingMargin = currentMargin;
                        }
                    }
                }
                else
                {
                    StopTrailingBuy(pair);
                }
            }

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TradingPairsProcessed, 
                $"Pairs: {traidingPairsCount}, Trailing buys: {trailingBuys.Count}, Trailing sells: {trailingSells.Count}");
        }

        public List<string> GetTrailingBuys()
        {
            return trailingBuys.Keys.ToList();
        }

        public List<string> GetTrailingSells()
        {
            return trailingSells.Keys.ToList();
        }

        public void StopTrailing()
        {
            trailingBuys.Clear();
            trailingSells.Clear();
        }

        public void StopTrailingBuy(string pair)
        {
            trailingBuys.TryRemove(pair, out BuyTrailingInfo buyTrailingInfo);
        }

        public void StopTrailingSell(string pair)
        {
            trailingSells.TryRemove(pair, out SellTrailingInfo sellTrailingInfo);
        }
    }
}
