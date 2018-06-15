using IntelliTrader.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IntelliTrader.Trading
{
    internal class ExchangeAccount : TradingAccountBase
    {
        public ExchangeAccount(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, ISignalsService signalsService, ITradingService tradingService)
            : base(loggingService, notificationService, healthCheckService, signalsService, tradingService)
        {

        }

        public override void Refresh()
        {
            loggingService.Info("Refresh account...");

            decimal newBalance = 0;
            Dictionary<string, decimal> availableAmounts = new Dictionary<string, decimal>();
            Dictionary<string, IEnumerable<IOrderDetails>> availableTrades = new Dictionary<string, IEnumerable<IOrderDetails>>();
            DateTimeOffset refreshStart = DateTimeOffset.Now;

            // Preload account data without locking the account
            try
            {
                loggingService.Info("Load account data...");

                foreach (var kvp in tradingService.GetAvailableAmounts())
                {
                    string currency = kvp.Key;
                    string pair = currency + tradingService.Config.Market;
                    decimal amount = kvp.Value;
                    decimal price = tradingService.GetCurrentPrice(pair);
                    decimal cost = amount * price;

                    if (currency == tradingService.Config.Market)
                    {
                        newBalance = amount;
                    }
                    else if (cost > tradingService.Config.MinCost && !tradingService.Config.ExcludedPairs.Contains(pair))
                    {
                        try
                        {
                            IEnumerable<IOrderDetails> trades = tradingService.GetMyTrades(pair);
                            availableTrades.Add(pair, trades);
                            availableAmounts.Add(pair, amount);
                        }
                        catch (Exception ex) when (ex.Message != null && ex.Message.Contains("Invalid symbol"))
                        {
                            loggingService.Info($"Skip invalid pair: {pair}");
                        }
                    }
                }

                loggingService.Info("Account data loaded");
            }
            catch (Exception ex) when (!isInitialRefresh)
            {
                healthCheckService.UpdateHealthCheck(Constants.HealthChecks.AccountRefreshed, ex.Message, true);
                loggingService.Error("Unable to load account data", ex);
                notificationService.Notify("Unable to load account data");
                return;
            }

            // Lock the account and reapply all trades
            try
            {
                lock (SyncRoot)
                {
                    ConcurrentDictionary<string, TradingPair> tradingPairsBackup = null;
                    if (isInitialRefresh)
                    {
                        TradingAccountData data = LoadBackupData();
                        tradingPairsBackup = data?.TradingPairs ?? new ConcurrentDictionary<string, TradingPair>();
                    }
                    else
                    {
                        tradingPairsBackup = tradingPairs;
                    }
                    tradingPairs = new ConcurrentDictionary<string, TradingPair>();

                    foreach (var kvp in availableTrades)
                    {
                        string pair = kvp.Key;
                        decimal amount = availableAmounts[pair];
                        IEnumerable<IOrderDetails> trades = kvp.Value;

                        foreach (var trade in trades)
                        {
                            if (trade.Date >= tradingService.Config.AccountInitialBalanceDate)
                            {
                                if (trade.Side == OrderSide.Buy)
                                {
                                    AddBuyOrder(trade);
                                }
                                else
                                {
                                    ITradeResult tradeResult = AddSellOrder(trade);
                                }

                                if (isInitialRefresh)
                                {
                                    tradingService.LogOrder(trade);
                                }
                            }
                        }

                        if (tradingPairs.TryGetValue(pair, out TradingPair tradingPair) && tradingPair.TotalAmount != amount)
                        {
                            loggingService.Info($"Fix amount for {pair}: {tradingPair.TotalAmount:0.########} => {amount:0.########}");
                            tradingPair.TotalAmount = amount;
                        }
                    }

                    foreach (var pair in tradingPairs.Keys.ToList())
                    {
                        if (tradingPairs[pair].AverageCostPaid <= tradingService.Config.MinCost)
                        {
                            loggingService.Info($"Skip low value pair: {pair}");
                            tradingPairs.TryRemove(pair, out TradingPair p);
                        }
                        else
                        {
                            if (tradingPairsBackup.TryGetValue(pair, out TradingPair backup))
                            {
                                tradingPairs[pair].Metadata = backup.Metadata ?? new OrderMetadata();
                            }
                        }
                    }

                    balance = newBalance;

                    // Add trades that were completed during account refresh
                    foreach (var order in tradingService.OrderHistory)
                    {
                        if (order.Date > refreshStart)
                        {
                            if (tradingPairs.TryGetValue(order.Pair, out TradingPair tradingPair))
                            {
                                if (!tradingPair.OrderIds.Contains(order.OrderId))
                                {
                                    loggingService.Info($"Add missing order for {order.Pair} ({order.OrderId})");
                                    AddOrder(order);
                                }
                            }
                            else
                            {
                                loggingService.Info($"Add missing order for {order.Pair} ({order.OrderId})");
                                AddOrder(order);
                            }
                        }
                    }

                    if (isInitialRefresh)
                    {
                        isInitialRefresh = false;
                        Save();
                    }

                    loggingService.Info($"Account refreshed. Balance: {balance}, Trading pairs: {tradingPairs.Count}");
                    healthCheckService.UpdateHealthCheck(Constants.HealthChecks.AccountRefreshed, $"Balance: {balance}, Trading pairs: {tradingPairs.Count}");
                }
            }
            catch (Exception ex)
            {
                tradingPairs.Clear();
                tradingService.SuspendTrading();
                healthCheckService.UpdateHealthCheck(Constants.HealthChecks.AccountRefreshed, ex.Message, true);
                loggingService.Error("Unable to refresh account", ex);
                notificationService.Notify("Unable to refresh account");
            }
        }

        public override void Save()
        {
            lock (SyncRoot)
            {
                try
                {
                    var tradingService = Application.Resolve<ITradingService>();
                    string accountFilePath = Path.Combine(Directory.GetCurrentDirectory(), tradingService.Config.AccountFilePath);

                    var data = new TradingAccountData
                    {
                        Balance = balance,
                        TradingPairs = tradingPairs,
                    };

                    string accountJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                    var accountFile = new FileInfo(accountFilePath);
                    accountFile.Directory.Create();
                    File.WriteAllText(accountFile.FullName, accountJson);
                }
                catch (Exception ex)
                {
                    loggingService.Error("Unable to save account backup data", ex);
                }
            }
        }

        private TradingAccountData LoadBackupData()
        {
            lock (SyncRoot)
            {
                try
                {
                    var tradingService = Application.Resolve<ITradingService>();
                    string accountFilePath = Path.Combine(Directory.GetCurrentDirectory(), tradingService.Config.AccountFilePath);

                    if (File.Exists(accountFilePath))
                    {
                        string accountJson = File.ReadAllText(accountFilePath);
                        return JsonConvert.DeserializeObject<TradingAccountData>(accountJson);
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    loggingService.Error("Unable to load account backup data", ex);
                    return null;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.AccountRefreshed);
        }
    }
}
