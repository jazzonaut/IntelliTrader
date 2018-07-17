using System;
using System.Collections.Generic;

namespace IntelliTrader.Core
{
    public interface IExchangeService : IConfigurableService
    {
        void Start(bool virtualTrading);
        void Stop();
        IOrderDetails PlaceOrder(IOrder order, string originalPair = null);
        decimal ClampOrderAmount(string pair, decimal amount);
        decimal ClampOrderPrice(string pair, decimal price);
        void ConnectTickersWebsocket();
        void DisconnectTickersWebsocket();
        IEnumerable<ITicker> GetTickers();
        IEnumerable<string> GetMarkets();
        IEnumerable<string> GetMarketPairs(string market);
        Dictionary<string, decimal> GetAvailableAmounts();
        IEnumerable<IOrderDetails> GetTrades(string pair);
        decimal GetPrice(string pair, TradePriceType priceType);
        decimal GetPriceSpread(string pair);
        Arbitrage GetArbitrage(string pair, string tradingMarket, ArbitrageMarket? arbitrageMarket = null, ArbitrageType? arbitrageType = null);
        string GetArbitrageMarketPair(ArbitrageMarket arbitrageMarket);
        string GetPairMarket(string pair);
        string ChangeMarket(string pair, string market);
        decimal ConvertPrice(string pair, decimal price, string market, TradePriceType priceType);
        TimeSpan GetTimeElapsedSinceLastTickersUpdate();
    }
}
