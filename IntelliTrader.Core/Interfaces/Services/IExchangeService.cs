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
        Task<IEnumerable<ITicker>> GetTickers(string market);
        Task<IEnumerable<string>> GetMarketPairs(string market);
        Task<Dictionary<string, decimal>> GetAvailableAmounts();
        Task<IEnumerable<IOrderDetails>> GetMyTrades(string pair);
        Task<decimal> GetLastPrice(string pair);
        Task<IOrderDetails> PlaceOrder(IOrder order);
    }
}
