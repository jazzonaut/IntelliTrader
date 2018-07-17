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
        IExchangeService Exchange { get; }
        ITradingAccount Account { get; }
        ConcurrentStack<IOrderDetails> OrderHistory { get; }
        bool IsTradingSuspended { get; }
        void Start();
        void Stop();
        void ResumeTrading(bool forced = false);
        void SuspendTrading(bool forced = false);
        IPairConfig GetPairConfig(string pair);
        void ReapplyTradingRules();
        void Buy(BuyOptions options);
        void Sell(SellOptions options);
        void Swap(SwapOptions options);
        void ArbitrageDirect(ArbitrageOptions options);
        void ArbitrageReverse(ArbitrageOptions options);
        bool CanBuy(BuyOptions options, out string message);
        bool CanSell(SellOptions options, out string message);
        bool CanSwap(SwapOptions options, out string message);
        bool CanArbitrage(ArbitrageOptions options, out string message);
        decimal GetPrice(string pair, TradePriceType? priceType = null);
        decimal CalculateOrderFees(IOrderDetails order);
        bool IsNormalizedPair(string pair);
        string NormalizePair(string pair);
        void LogOrder(IOrderDetails order);
        List<string> GetTrailingBuys();
        List<string> GetTrailingSells();
        void StopTrailingBuy(string pair);
        void StopTrailingSell(string pair);
    }
}
