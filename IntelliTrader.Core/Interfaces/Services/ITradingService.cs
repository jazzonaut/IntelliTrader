using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntelliTrader.Core
{
    public interface ITradingService : IConfigurableService
    {
        ITradingConfig Config { get; }
        IModuleRules Rules { get; }
        bool IsTradingSuspended { get; }
        void Start();
        void Stop();
        void ResumeTrading(bool forced = false);
        void SuspendTrading(bool forced = false);
        ITradingAccount Account { get; }
        ConcurrentStack<IOrderDetails> OrderHistory { get; }
        IPairConfig GetPairConfig(string pair);
        void ReapplyTradingRules();
        void Buy(BuyOptions options);
        void Sell(SellOptions options);
        void Swap(SwapOptions options);
        bool CanBuy(BuyOptions options, out string message);
        bool CanSell(SellOptions options, out string message);
        bool CanSwap(SwapOptions options, out string message);
        void LogOrder(IOrderDetails order);
        List<string> GetTrailingBuys();
        List<string> GetTrailingSells();
        IEnumerable<ITicker> GetTickers();
        IEnumerable<string> GetMarketPairs();
        Dictionary<string, decimal> GetAvailableAmounts();
        IEnumerable<IOrderDetails> GetMyTrades(string pair);
        IOrderDetails PlaceOrder(IOrder order);
        decimal GetCurrentPrice(string pair);
    }
}
