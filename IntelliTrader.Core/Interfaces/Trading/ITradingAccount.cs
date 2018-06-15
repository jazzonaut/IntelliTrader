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
        decimal GetBalance();
        bool HasTradingPair(string pair);
        ITradingPair GetTradingPair(string pair);
        IEnumerable<ITradingPair> GetTradingPairs();
    }
}
