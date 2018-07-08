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
        Task<IEnumerable<string>> GetMarkets();
        Task<IEnumerable<ITicker>> GetTickers();
        Task<IEnumerable<string>> GetMarketPairs(string market);
        Task<Dictionary<string, decimal>> GetAvailableAmounts();
        Task<IEnumerable<IOrderDetails>> GetMyTrades(string pair);
        Task<decimal> GetAskPrice(string pair);
        Task<decimal> GetBidPrice(string pair);
        Task<decimal> GetLastPrice(string pair);
        Task<decimal> GetPriceSpread(string pair);
        Task<decimal> GetPriceArbitrage(string pair, string crossMarket, string market);
        Task<IOrderDetails> PlaceOrder(IOrder order);
    }
}
