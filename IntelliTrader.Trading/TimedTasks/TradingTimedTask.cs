using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Trading
{
    internal class TradingTimedTask : HighResolutionTimedTask
    {
        public bool LoggingEnabled { get; set; } = true;

        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ISignalsService signalsService;
        private readonly TradingService tradingService;

        private readonly ConcurrentDictionary<string, BuyTrailingInfo> trailingBuys = new ConcurrentDictionary<string, BuyTrailingInfo>();
        private readonly ConcurrentDictionary<string, SellTrailingInfo> trailingSells = new ConcurrentDictionary<string, SellTrailingInfo>();

        public TradingTimedTask(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, ISignalsService signalsService, TradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.signalsService = signalsService;
            this.tradingService = tradingService;
        }

        public override void Run()
        {
            lock (tradingService.SyncRoot)
            {
                ProcessTradingPairs();
            }
        }

        public void ClearTrailing()
        {
            trailingBuys.Clear();
            trailingSells.Clear();
        }

        public List<string> GetTrailingBuys()
        {
            return trailingBuys.Keys.ToList();
        }

        public List<string> GetTrailingSells()
        {
            return trailingSells.Keys.ToList();
        }

        public void InitiateBuy(BuyOptions options)
        {
            IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);

            if (!options.ManualOrder && !options.Swap && pairConfig.BuyTrailing != 0)
            {
                if (!trailingBuys.ContainsKey(options.Pair))
                {
                    trailingSells.TryRemove(options.Pair, out SellTrailingInfo sellTrailingInfo);

                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                    decimal currentPrice = tradingService.GetCurrentPrice(options.Pair);
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
                            loggingService.Info($"Start trailing buy {tradingPair?.FormattedName ?? options.Pair}. Price: {currentPrice:0.00000000}, Margin: {currentMargin:0.00}");
                        }
                    }
                }
                else
                {
                    if (LoggingEnabled)
                    {
                        //loggingService.Info($"Cancel trailing buy {tradingPair?.FormattedName ?? pair}. Reason: already trailing");
                    }
                }
            }
            else
            {
                PlaceBuyOrder(options);
            }
        }

        public void InitiateSell(SellOptions options)
        {
            IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);

            if (!options.ManualOrder && !options.Swap && pairConfig.SellTrailing != 0)
            {
                if (!trailingSells.ContainsKey(options.Pair))
                {
                    trailingBuys.TryRemove(options.Pair, out BuyTrailingInfo buyTrailingInfo);

                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                    tradingPair.SetCurrentPrice(tradingService.GetCurrentPrice(options.Pair));

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
                            loggingService.Info($"Start trailing sell {tradingPair.FormattedName}. Price: {tradingPair.CurrentPrice:0.00000000}, Margin: {tradingPair.CurrentMargin:0.00}");
                        }
                    }
                }
                else
                {
                    if (LoggingEnabled)
                    {
                        //loggingService.Info($"Cancel trailing sell {tradingPair.FormattedName}. Reason: already trailing");
                    }
                }
            }
            else
            {
                PlaceSellOrder(options);
            }
        }

        private void ProcessTradingPairs()
        {
            int traidingPairsCount = 0;

            foreach (var tradingPair in tradingService.Account.GetTradingPairs())
            {
                IPairConfig pairConfig = tradingService.GetPairConfig(tradingPair.Pair);
                tradingPair.SetCurrentPrice(tradingService.GetCurrentPrice(tradingPair.Pair));
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
                                loggingService.Info($"Continue trailing sell {tradingPair.FormattedName}. Price: {tradingPair.CurrentPrice:0.00000000}, Margin: {tradingPair.CurrentMargin:0.00}");
                            }
                        }

                        if (tradingPair.CurrentMargin <= sellTrailingInfo.TrailingStopMargin || tradingPair.CurrentMargin < (sellTrailingInfo.BestTrailingMargin - sellTrailingInfo.Trailing))
                        {
                            trailingSells.TryRemove(tradingPair.Pair, out SellTrailingInfo p);

                            if (tradingPair.CurrentMargin > 0 || sellTrailingInfo.SellMargin < 0)
                            {
                                if (sellTrailingInfo.TrailingStopAction == SellTrailingStopAction.Sell || tradingPair.CurrentMargin > sellTrailingInfo.TrailingStopMargin)
                                {
                                    PlaceSellOrder(sellTrailingInfo.SellOptions);
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
                        trailingSells.TryRemove(tradingPair.Pair, out SellTrailingInfo p);
                    }
                }
                else
                {
                    if (pairConfig.SellEnabled && tradingPair.CurrentMargin >= pairConfig.SellMargin)
                    {
                        InitiateSell(new SellOptions(tradingPair.Pair));
                    }
                    else if (pairConfig.SellEnabled && pairConfig.SellStopLossEnabled && tradingPair.CurrentMargin <= pairConfig.SellStopLossMargin && tradingPair.CurrentAge >= pairConfig.SellStopLossMinAge &&
                        (pairConfig.NextDCAMargin == null || !pairConfig.SellStopLossAfterDCA))
                    {
                        if (LoggingEnabled)
                        {
                            loggingService.Info($"Stop loss triggered for {tradingPair.FormattedName}. Margin: {tradingPair.CurrentMargin:0.00}");
                        }
                        PlaceSellOrder(new SellOptions(tradingPair.Pair));
                    }
                    else if (pairConfig.NextDCAMargin != null && pairConfig.BuyEnabled && pairConfig.NextDCAMargin != null &&
                        !trailingBuys.ContainsKey(tradingPair.Pair) && !trailingSells.ContainsKey(tradingPair.Pair))
                    {
                        if (tradingPair.CurrentMargin <= pairConfig.NextDCAMargin)
                        {
                            var buyOptions = new BuyOptions(tradingPair.Pair)
                            {
                                MaxCost = tradingPair.AverageCostPaid * pairConfig.BuyMultiplier,
                                IgnoreExisting = true
                            };

                            if (tradingService.CanBuy(buyOptions, message: out string message))
                            {
                                if (LoggingEnabled)
                                {
                                    loggingService.Info($"DCA triggered for {tradingPair.FormattedName}. Margin: {tradingPair.CurrentMargin:0.00}, Level: {pairConfig.NextDCAMargin:0.00}, Multiplier: {pairConfig.BuyMultiplier}");
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
                decimal currentPrice = tradingService.GetCurrentPrice(pair);
                decimal currentMargin = Utils.CalculateMargin(buyTrailingInfo.InitialPrice, currentPrice);

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
                        trailingBuys.TryRemove(pair, out BuyTrailingInfo p);

                        if (buyTrailingInfo.TrailingStopAction == BuyTrailingStopAction.Buy || currentMargin < buyTrailingInfo.TrailingStopMargin)
                        {
                            PlaceBuyOrder(buyTrailingInfo.BuyOptions);
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
                    trailingBuys.TryRemove(pair, out BuyTrailingInfo p);
                }
            }

            healthCheckService.UpdateHealthCheck(Constants.HealthChecks.TradingPairsProcessed, $"Pairs: {traidingPairsCount}, Trailing buys: {trailingBuys.Count}, Trailing sells: {trailingSells.Count}");
        }

        public IOrderDetails PlaceBuyOrder(BuyOptions options)
        {
            IOrderDetails orderDetails = null;
            trailingBuys.TryRemove(options.Pair, out BuyTrailingInfo buyTrailingInfo);
            trailingSells.TryRemove(options.Pair, out SellTrailingInfo sellTrailingInfo);

            if (tradingService.CanBuy(options, out string message))
            {
                IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
                ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                decimal currentPrice = tradingService.GetCurrentPrice(options.Pair);
                options.Metadata.TradingRules = pairConfig.Rules.ToList();
                if (options.Metadata.LastBuyMargin == null)
                {
                    options.Metadata.LastBuyMargin = tradingPair?.CurrentMargin ?? 0;
                }
                string signalRule = options.Metadata.SignalRule ?? "N/A";

                BuyOrder buyOrder = new BuyOrder
                {
                    Type = pairConfig.BuyType,
                    Date = DateTimeOffset.Now,
                    Pair = options.Pair,
                    Amount = options.Amount ?? (options.MaxCost.Value / currentPrice),
                    Price = currentPrice
                };

                if (!tradingService.Config.VirtualTrading)
                {
                    loggingService.Info($"Place buy order for {tradingPair?.FormattedName ?? options.Pair}. Price: {buyOrder.Price:0.00000000}, Amount: {buyOrder.Amount:0.########}, Signal Rule: {signalRule}");

                    try
                    {
                        lock (tradingService.Account.SyncRoot)
                        {
                            orderDetails = tradingService.PlaceOrder(buyOrder);
                            orderDetails.SetMetadata(options.Metadata);
                            tradingService.Account.AddBuyOrder(orderDetails);
                            tradingService.Account.Save();
                            tradingService.LogOrder(orderDetails);

                            tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                            loggingService.Info("{@Trade}", orderDetails);
                            loggingService.Info($"Buy order result for {tradingPair.FormattedName}: {orderDetails.Result} ({orderDetails.Message}). Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Filled: {orderDetails.AmountFilled:0.########}, Cost: {orderDetails.AverageCost:0.00000000}");
                            notificationService.Notify($"Bought {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Cost: {orderDetails.AverageCost:0.00000000}");
                        }
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error($"Unable to place buy order for {options.Pair}", ex);
                        notificationService.Notify($"Unable to buy {options.Pair}: {ex.Message}");
                    }
                }
                else
                {
                    loggingService.Info($"Place virtual buy order for {tradingPair?.FormattedName ?? options.Pair}. Price: {buyOrder.Price:0.00000000}, Amount: {buyOrder.Amount:0.########}, Signal Rule: {signalRule}");

                    lock (tradingService.Account.SyncRoot)
                    {
                        decimal roundedAmount = Math.Round(buyOrder.Amount, 4);
                        orderDetails = new OrderDetails
                        {
                            Metadata = options.Metadata,
                            OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                            Side = OrderSide.Buy,
                            Result = OrderResult.Filled,
                            Date = buyOrder.Date,
                            Pair = buyOrder.Pair,
                            Amount = roundedAmount,
                            AmountFilled = roundedAmount,
                            Price = buyOrder.Price,
                            AveragePrice = buyOrder.Price
                        };
                        tradingService.Account.AddBuyOrder(orderDetails);
                        tradingService.Account.Save();
                        tradingService.LogOrder(orderDetails);

                        tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                        loggingService.Info("{@Trade}", orderDetails);
                        loggingService.Info($"Virtual buy order result for {tradingPair.FormattedName}. Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Cost: {orderDetails.AverageCost:0.00000000}");
                        notificationService.Notify($"Bought {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Cost: {orderDetails.AverageCost:0.00000000}");
                    }
                }

                tradingService.ReapplyTradingRules();
            }
            else
            {
                loggingService.Info(message);
            }
            return orderDetails;
        }

        public IOrderDetails PlaceSellOrder(SellOptions options)
        {
            IOrderDetails orderDetails = null;
            trailingSells.TryRemove(options.Pair, out SellTrailingInfo sellTrailingInfo);
            trailingBuys.TryRemove(options.Pair, out BuyTrailingInfo buyTrailingInfo);

            if (tradingService.CanSell(options, out string message))
            {
                IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
                ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                tradingPair.SetCurrentPrice(tradingService.GetCurrentPrice(options.Pair));

                SellOrder sellOrder = new SellOrder
                {
                    Type = pairConfig.SellType,
                    Date = DateTimeOffset.Now,
                    Pair = options.Pair,
                    Amount = options.Amount ?? tradingPair.TotalAmount,
                    Price = tradingPair.CurrentPrice
                };

                if (!tradingService.Config.VirtualTrading)
                {
                    loggingService.Info($"Place sell order for {tradingPair.FormattedName}. Price: {sellOrder.Price:0.00000000}, Amount: {sellOrder.Amount:0.########}, Margin: {tradingPair.CurrentMargin:0.00}");

                    try
                    {
                        lock (tradingService.Account.SyncRoot)
                        {
                            orderDetails = tradingService.PlaceOrder(sellOrder);
                            tradingPair.Metadata.SwapPair = options.SwapPair;
                            orderDetails.SetMetadata(tradingPair.Metadata);
                            ITradeResult tradeResult = tradingService.Account.AddSellOrder(orderDetails);
                            tradeResult.SetSwap(options.Swap);
                            tradingService.Account.Save();
                            tradingService.LogOrder(orderDetails);

                            decimal soldMargin = (tradeResult.Profit / (tradeResult.AverageCost + (tradeResult.Metadata.AdditionalCosts ?? 0)) * 100);
                            string swapPair = options.SwapPair != null ? $", Swap Pair: {options.SwapPair}" : "";
                            loggingService.Info("{@Trade}", orderDetails);
                            loggingService.Info("{@Trade}", tradeResult);
                            loggingService.Info($"Sell order result for {tradingPair.FormattedName}: {orderDetails.Result} ({orderDetails.Message}). Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Filled: {orderDetails.AmountFilled:0.########}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}");
                            notificationService.Notify($"Sold {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}{swapPair}");
                        }
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error($"Unable to place sell order for {options.Pair}", ex);
                        notificationService.Notify($"Unable to sell {options.Pair}: {ex.Message}");
                    }
                }
                else
                {
                    loggingService.Info($"Place virtual sell order for {tradingPair.FormattedName}. Price: {sellOrder.Price:0.00000000}, Amount: {sellOrder.Amount:0.########}");

                    lock (tradingService.Account.SyncRoot)
                    {
                        orderDetails = new OrderDetails
                        {
                            Metadata = tradingPair.Metadata,
                            OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                            Side = OrderSide.Sell,
                            Result = OrderResult.Filled,
                            Date = sellOrder.Date,
                            Pair = sellOrder.Pair,
                            Amount = sellOrder.Amount,
                            AmountFilled = sellOrder.Amount,
                            Price = sellOrder.Price,
                            AveragePrice = sellOrder.Price
                        };
                        tradingPair.Metadata.SwapPair = options.SwapPair;
                        ITradeResult tradeResult = tradingService.Account.AddSellOrder(orderDetails);
                        tradeResult.SetSwap(options.Swap);
                        tradingService.Account.Save();
                        tradingService.LogOrder(orderDetails);

                        decimal soldMargin = (tradeResult.Profit / (tradeResult.AverageCost + (tradeResult.Metadata.AdditionalCosts ?? 0)) * 100);
                        string swapPair = options.SwapPair != null ? $", Swap Pair: {options.SwapPair}" : "";
                        loggingService.Info("{@Trade}", orderDetails);
                        loggingService.Info("{@Trade}", tradeResult);
                        loggingService.Info($"Virtual sell order result for {tradingPair.FormattedName}. Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Margin: {tradingPair.CurrentMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}");
                        notificationService.Notify($"Sold {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Margin: {tradingPair.CurrentMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}{swapPair}");
                    }
                }

                tradingService.ReapplyTradingRules();
            }
            else
            {
                loggingService.Info(message);
            }
            return orderDetails;
        }
    }
}
