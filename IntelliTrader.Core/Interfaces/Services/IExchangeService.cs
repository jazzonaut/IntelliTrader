using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntelliTrader.Core
{
    public interface IExchangeService : IConfigurableService
    {
        void Start(bool virtualTrading);
        void Stop();
        IOrderDetails PlaceOrder(IOrder order, string priceCurrency = null);
        void ConnectTickersWebsocket();
        void DisconnectTickersWebsocket();
        IEnumerable<string> GetMarkets();
        IEnumerable<ITicker> GetTickers();
        IEnumerable<string> GetMarketPairs(string market);
        string GetPairMarket(string pair);
        string ChangePairMarket(string pair, string newMarket);
        Dictionary<string, decimal> GetAvailableAmounts();
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
