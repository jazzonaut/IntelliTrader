using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
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
        public abstract bool IsVirtual { get; }

        protected readonly ILoggingService loggingService;
        protected readonly INotificationService notificationService;
        protected readonly IHealthCheckService healthCheckService;
        protected readonly ISignalsService signalsService;
        protected readonly ITradingService tradingService;

        protected bool isInitialRefresh = true;
        protected decimal balance;
        protected ConcurrentDictionary<string, TradingPair> tradingPairs = new ConcurrentDictionary<string, TradingPair>();

        public TradingAccountBase(ILoggingService loggingService, INotificationService notificationService,
            IHealthCheckService healthCheckService, ISignalsService signalsService, ITradingService tradingService)
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
                    string feesPair = order.FeesCurrency + tradingService.Config.Market;
                    decimal feesPairCurrency = 0;
                    decimal feesMarketCurrency = tradingService.CalculateOrderFees(order);
                    decimal amountAfterFees = order.AmountFilled;
                    decimal balanceOffset = 0;

                    if (!order.IsNormalized || order.Pair.EndsWith(Constants.Markets.USDT))
                    {
                        balanceOffset = -order.RawCost;
                    }
                    else
                    {
                        string normalizedMarket = tradingService.Exchange.GetPairMarket(order.OriginalPair) == Constants.Markets.USDT ?
                            tradingService.Config.Market + tradingService.Exchange.GetPairMarket(order.OriginalPair) :
                            tradingService.Exchange.GetPairMarket(order.OriginalPair) + tradingService.Config.Market;

                        if (tradingPairs.TryGetValue(normalizedMarket, out TradingPair normalizedMarketPair))
                        {
                            if (normalizedMarketPair.RawCost > order.RawCost)
                            {
                                normalizedMarketPair.Amount -= order.RawCost / tradingService.GetPrice(normalizedMarket, TradePriceType.Ask);
                                if (normalizedMarketPair.Amount <= 0)
                                {
                                    tradingPairs.TryRemove(normalizedMarket, out normalizedMarketPair);
                                    if (normalizedMarketPair.Amount < 0)
                                    {
                                        loggingService.Error($"Normalized pair {normalizedMarket} has negative amount: {normalizedMarketPair.Amount}");
                                    }
                                }
                            }
                            else
                            {
                                tradingPairs.TryRemove(normalizedMarket, out normalizedMarketPair);
                            }
                        }
                        else
                        {
                            loggingService.Error($"Unable to get normalized pair {normalizedMarketPair}");
                        }
                    }

                    if (order.FeesCurrency == tradingService.Config.Market)
                    {
                        balanceOffset -= feesMarketCurrency;
                    }
                    else
                    {
                        if (feesPair == order.Pair)
                        {
                            feesPairCurrency = order.Fees;
                            amountAfterFees -= order.Fees;
                        }
                    }
                    AddBalance(balanceOffset);

                    if (tradingPairs.TryGetValue(order.Pair, out TradingPair tradingPair))
                    {
                        if (!tradingPair.OrderIds.Contains(order.OrderId))
                        {
                            tradingPair.OrderIds.Add(order.OrderId);
                            tradingPair.OrderDates.Add(order.Date);
                        }
                        tradingPair.AveragePrice = (tradingPair.ActualCost + order.RawCost) / (tradingPair.Amount + order.AmountFilled);
                        tradingPair.FeesPairCurrency += feesPairCurrency;
                        tradingPair.FeesMarketCurrency += feesMarketCurrency;
                        tradingPair.Amount += amountAfterFees;
                        tradingPair.Metadata.MergeWith(order.Metadata);
                    }
                    else
                    {
                        tradingPair = new TradingPair
                        {
                            Pair = order.Pair,
                            OrderIds = new List<string> { order.OrderId },
                            OrderDates = new List<DateTimeOffset> { order.Date },
                            AveragePrice = order.AveragePrice + (feesMarketCurrency / order.AmountFilled),
                            FeesPairCurrency = feesPairCurrency,
                            FeesMarketCurrency = feesMarketCurrency,
                            Amount = amountAfterFees,
                            Metadata = order.Metadata
                        };
                        tradingPairs.TryAdd(order.Pair, tradingPair);
                        tradingPair.SetCurrentValues(tradingService.GetPrice(tradingService.NormalizePair(tradingPair.Pair)), tradingService.Exchange.GetPriceSpread(tradingPair.Pair));
                        tradingPair.Metadata.CurrentRating = tradingPair.Metadata.Signals != null ? signalsService.GetRating(tradingPair.Pair, tradingPair.Metadata.Signals) : null;
                        tradingPair.Metadata.CurrentGlobalRating = signalsService.GetGlobalRating();
                    }
                }
            }
        }

        public virtual ITradeResult AddSellOrder(IOrderDetails order)
        {
            ITradeResult tradeResult = new TradeResult();
            lock (SyncRoot)
            {
                if (tradingPairs.TryGetValue(order.Pair, out TradingPair tradingPair))
                {
                    if (order.Side == OrderSide.Sell && (order.Result == OrderResult.Filled || order.Result == OrderResult.FilledPartially))
                    {
                        string feesPair = order.FeesCurrency + tradingService.Config.Market;
                        decimal feesPairCurrency = (feesPair == order.Pair) ? order.Fees : 0;
                        decimal feesMarketCurrency = tradingService.CalculateOrderFees(order);
                        decimal amountDifference = order.AmountFilled / tradingPair.Amount;
                        decimal balanceOffset = order.RawCost;

                        if (order.FeesCurrency == tradingService.Config.Market)
                        {
                            balanceOffset -= order.Fees;
                        }
                        AddBalance(balanceOffset);

                        tradingPair.FeesMarketCurrency += feesMarketCurrency;
                        tradingPair.FeesMarketCurrency -= tradingPair.FeesMarketCurrency * amountDifference;

                        decimal costDifference = order.RawCost - tradingPair.GetActualCost(order.AmountFilled) - (tradingPair.Metadata.AdditionalCosts ?? 0);
                        decimal profit = costDifference * amountDifference;

                        if (tradingPair.Amount > order.AmountFilled)
                        {
                            tradingPair.Amount -= order.AmountFilled;
                        }
                        else
                        {
                            tradingPairs.TryRemove(order.Pair, out tradingPair);
                        }

                        tradeResult = new TradeResult
                        {
                            IsSuccessful = true,
                            Pair = order.Pair,
                            Amount = order.AmountFilled,
                            OrderDates = tradingPair.OrderDates,
                            AveragePrice = tradingPair.AveragePrice,
                            FeesPairCurrency = feesPairCurrency,
                            FeesMarketCurrency = feesMarketCurrency,
                            SellDate = order.Date,
                            SellPrice = order.AveragePrice,
                            BalanceOffset = balanceOffset,
                            Profit = profit,
                            Metadata = order.Metadata,
                        };
                    }
                }
            }
            return tradeResult;
        }

        public IOrderDetails AddBlankOrder(string pair, decimal amount, bool includeFees = true)
        {
            lock (SyncRoot)
            {
                if (tradingPairs.TryGetValue(pair, out TradingPair tradingPair) && tradingPair.Amount >= amount)
                {
                    if (tradingPair.Amount > amount)
                    {
                        return new OrderDetails
                        {
                            OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                            Side = OrderSide.Sell,
                            Result = OrderResult.Filled,
                            Date = DateTimeOffset.Now,
                            Pair = pair,
                            Amount = amount,
                            AmountFilled = amount,
                            Price = tradingPair.AveragePrice,
                            AveragePrice = tradingPair.AveragePrice,
                            Fees = includeFees ? tradingPair.FeesTotal * (amount / tradingPair.Amount) : 0,
                            FeesCurrency = includeFees ? tradingService.Config.Market : null,
                            Metadata = tradingPair.Metadata
                        };
                    }
                    else
                    {
                        return new OrderDetails();
                    }
                }
                else
                {
                    return new OrderDetails();
                }
            }
        }

        public void AddBalance(decimal balanceOffset)
        {
            balance += balanceOffset;
        }

        public decimal GetBalance()
        {
            lock (SyncRoot)
            {
                return balance;
            }
        }

        public decimal GetTotalBalance()
        {
            decimal totalBalance = balance;
            foreach (var tradingPair in tradingPairs.Values)
            {
                totalBalance += tradingService.GetPrice(tradingPair.Pair, TradePriceType.Bid) * tradingPair.Amount;
            }
            return totalBalance;
        }

        public bool HasTradingPair(string pair, bool includeDust = false)
        {
            lock (SyncRoot)
            {
                if (includeDust)
                {
                    return tradingPairs.ContainsKey(pair);
                }
                else
                {
                    return tradingPairs.TryGetValue(pair, out TradingPair tradingPair) && (tradingPair.CurrentCost > tradingService.Config.MinCost || tradingPair.CurrentPrice == 0);
                }
            }
        }

        public ITradingPair GetTradingPair(string pair, bool includeDust = false)
        {
            lock (SyncRoot)
            {
                if (tradingPairs.TryGetValue(pair, out TradingPair tradingPair) && (includeDust || tradingPair.CurrentCost > tradingService.Config.MinCost || tradingPair.CurrentPrice == 0))
                {
                    return tradingPair;
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<ITradingPair> GetTradingPairs(bool includeDust = false)
        {
            lock (SyncRoot)
            {
                if (includeDust)
                {
                    return tradingPairs.Values;
                }
                else
                {
                    return tradingPairs.Values.Where(t => t.CurrentCost > tradingService.Config.MinCost || t.CurrentPrice == 0);
                }
            }
        }

        public virtual void Dispose()
        {
            lock (SyncRoot)
            {
                tradingPairs.Clear();
            }
        }
    }
}
