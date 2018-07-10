using System;
using System.Collections.Generic;

namespace IntelliTrader.Core
{
    public interface IExchangeService : IConfigurableService
    {
        void Start(bool virtualTrading);
        void Stop();
        IOrderDetails PlaceOrder(IOrder order, string priceCurrency = null);
        void ConnectTickersWebsocket();
        void DisconnectTickersWebsocket();
        Dictionary<string, decimal> GetAvailableAmounts();
        IEnumerable<ITicker> GetTickers();
        IEnumerable<string> GetMarkets();
        IEnumerable<string> GetMarketPairs(string market);
        string GetPairMarket(string pair);
        string ChangePairMarket(string pair, string market);
        decimal ConvertPairPrice(string pair, decimal price, string market);
        IEnumerable<IOrderDetails> GetTrades(string pair);
        decimal GetAskPrice(string pair);
        decimal GetBidPrice(string pair);
        decimal GetLastPrice(string pair);
        decimal GetPriceSpread(string pair);
        decimal GetPriceArbitrage(string pair, string crossMarket, string market);
        string GetArbitrageMarket(string crossMarket);
        TimeSpan GetTimeElapsedSinceLastTickersUpdate();
    }
}
