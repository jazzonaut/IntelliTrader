using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITradingAccount : IDisposable
    {
        object SyncRoot { get; }
        void Refresh();
        void Save();
        void AddOrder(IOrderDetails order);
        void AddBuyOrder(IOrderDetails order);
        ITradeResult AddSellOrder(IOrderDetails order);
        ITradingPair AddOrUpdatePair(IOrderDetails order, string pair, decimal feesMarketCurrency, decimal feesPairCurrency, decimal? amountOverride = null, decimal? averagePriceOverride = null);
        IOrderDetails AddBlankOrder(string pair, decimal amount, bool includeFees = true);
        void AddBalance(decimal balanceOffset);
        decimal GetBalance();
        decimal GetTotalBalance();
        bool HasTradingPair(string pair, bool includeDust = false);
        ITradingPair GetTradingPair(string pair, bool includeDust = false);
        IEnumerable<ITradingPair> GetTradingPairs(bool includeDust = false);
    }
}
