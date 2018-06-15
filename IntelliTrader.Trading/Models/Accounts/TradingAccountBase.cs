using IntelliTrader.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliTrader.Trading
{
    internal abstract class TradingAccountBase : ITradingAccount
    {
        public object SyncRoot { get; private set; } = new object();

        protected readonly ILoggingService loggingService;
        protected readonly INotificationService notificationService;
        protected readonly IHealthCheckService healthCheckService;
        protected readonly ISignalsService signalsService;
        protected readonly ITradingService tradingService;

        protected bool isInitialRefresh = true;
        protected decimal balance;
        protected ConcurrentDictionary<string, TradingPair> tradingPairs = new ConcurrentDictionary<string, TradingPair>();

        public TradingAccountBase(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, ISignalsService signalsService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.signalsService = signalsService;
            this.tradingService = tradingService;
        }

        public abstract void Refresh();

        public abstract void Save();

        public virtual void AddOrder(IOrderDetails order)
        {
            if (order.Side == OrderSide.Buy)
            {
                AddBuyOrder(order);
            }
            else
            {
                AddSellOrder(order);
            }
        }

        public virtual void AddBuyOrder(IOrderDetails order)
        {
            lock (SyncRoot)
            {
                if (order.Side == OrderSide.Buy && (order.Result == OrderResult.Filled || order.Result == OrderResult.FilledPartially))
                {
                    decimal balanceDifference = -order.AverageCost;
                    decimal feesPairCurrency = 0;
                    decimal feesMarketCurrency = 0;
                    decimal amountAfterFees = order.AmountFilled;

                    if (order.Fees != 0 && order.FeesCurrency != null)
                    {
                        if (order.FeesCurrency == tradingService.Config.Market)
                        {
                            feesMarketCurrency = order.Fees;
                            balanceDifference -= order.Fees;
                        }
                        else
                        {
                            string feesPair = order.FeesCurrency + tradingService.Config.Market;
                            if (feesPair == order.Pair)
                            {
                                feesPairCurrency = order.Fees;
                                amountAfterFees -= order.Fees;
                            }
                            else
                            {
                                feesMarketCurrency = tradingService.GetCurrentPrice(feesPair) * order.Fees;
                            }
                        }
                    }
                    balance += balanceDifference;

                    if (tradingPairs.TryGetValue(order.Pair, out TradingPair tradingPair))
                    {
                        if (!tradingPair.OrderIds.Contains(order.OrderId))
                        {
                            tradingPair.OrderIds.Add(order.OrderId);
                            tradingPair.OrderDates.Add(order.Date);
                        }
                        tradingPair.AveragePricePaid = (tradingPair.AverageCostPaid + order.AverageCost) / (tradingPair.TotalAmount + order.AmountFilled);
                        tradingPair.FeesPairCurrency += feesPairCurrency;
                        tradingPair.FeesMarketCurrency += feesMarketCurrency;
                        tradingPair.TotalAmount += amountAfterFees;
                        tradingPair.Metadata = tradingPair.Metadata.MergeWith(order.Metadata);
                    }
                    else
                    {
                        tradingPair = new TradingPair
                        {
                            Pair = order.Pair,
                            OrderIds = new List<string> { order.OrderId },
                            OrderDates = new List<DateTimeOffset> { order.Date },
                            AveragePricePaid = order.AveragePrice,
                            FeesPairCurrency = feesPairCurrency,
                            FeesMarketCurrency = feesMarketCurrency,
                            TotalAmount = amountAfterFees,
                            Metadata = order.Metadata
                        };
                        tradingPairs.TryAdd(order.Pair, tradingPair);
                        tradingPair.SetCurrentPrice(tradingService.GetCurrentPrice(tradingPair.Pair));
                        tradingPair.Metadata.CurrentRating = tradingPair.Metadata.Signals != null ? signalsService.GetRating(tradingPair.Pair, tradingPair.Metadata.Signals) : null;
                        tradingPair.Metadata.CurrentGlobalRating = signalsService.GetGlobalRating();
                    }
                }
            }
        }

        public virtual ITradeResult AddSellOrder(IOrderDetails order)
        {
            lock (SyncRoot)
            {
                if (tradingPairs.TryGetValue(order.Pair, out TradingPair tradingPair))
                {
                    if (order.Side == OrderSide.Sell && (order.Result == OrderResult.Filled || order.Result == OrderResult.FilledPartially))
                    {
                        decimal balanceDifference = order.AverageCost;

                        if (order.Fees != 0 && order.FeesCurrency != null)
                        {
                            if (order.FeesCurrency == tradingService.Config.Market)
                            {
                                tradingPair.FeesMarketCurrency += order.Fees;
                                balanceDifference -= order.Fees;
                            }
                            else
                            {
                                string feesPair = order.FeesCurrency + tradingService.Config.Market;
                                tradingPair.FeesMarketCurrency += tradingService.GetCurrentPrice(feesPair) * order.Fees;
                            }
                        }
                        balance += balanceDifference;
                        decimal profit = (order.AverageCost - tradingPair.AverageCostPaid - (tradingPair.Metadata.AdditionalCosts ?? 0)) * (order.AmountFilled / tradingPair.TotalAmount);

                        var tradeResult = new TradeResult
                        {
                            IsSuccessful = true,
                            Metadata = order.Metadata,
                            Pair = order.Pair,
                            Amount = order.AmountFilled,
                            OrderDates = tradingPair.OrderDates,
                            AveragePricePaid = tradingPair.AveragePricePaid,
                            FeesPairCurrency = tradingPair.FeesPairCurrency,
                            FeesMarketCurrency = tradingPair.FeesMarketCurrency,
                            SellDate = order.Date,
                            SellPrice = order.AveragePrice,
                            BalanceDifference = balanceDifference,
                            Profit = profit
                        };

                        if (tradingPair.TotalAmount > order.AmountFilled)
                        {
                            tradingPair.TotalAmount -= order.AmountFilled;

                            if (!isInitialRefresh && tradingPair.AverageCostPaid <= tradingService.Config.MinCost)
                            {
                                tradingPairs.TryRemove(order.Pair, out tradingPair);
                            }
                        }
                        else
                        {
                            tradingPairs.TryRemove(order.Pair, out tradingPair);
                        }

                        return tradeResult;
                    }
                    else
                    {
                        return new TradeResult { IsSuccessful = false };
                    }
                }
                else
                {
                    return new TradeResult { IsSuccessful = false };
                }
            }
        }

        public decimal GetBalance()
        {
            lock (SyncRoot)
            {
                return balance;
            }
        }

        public bool HasTradingPair(string pair)
        {
            lock (SyncRoot)
            {
                return tradingPairs.ContainsKey(pair);
            }
        }

        public ITradingPair GetTradingPair(string pair)
        {
            lock (SyncRoot)
            {
                if (tradingPairs.TryGetValue(pair, out TradingPair tradingPair))
                {
                    return tradingPair;
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<ITradingPair> GetTradingPairs()
        {
            lock (SyncRoot)
            {
                return tradingPairs.Values;
            }
        }

        public virtual void Dispose()
        {
            lock (SyncRoot)
            {
                Save();
                tradingPairs.Clear();
            }
        }
    }
}
