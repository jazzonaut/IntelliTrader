using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using ZeroFormatter;

namespace IntelliTrader.Backtesting
{
    [ZeroFormattable]
    public class TickerData : ITicker
    {
        [Index(0)]
        public virtual string Pair { get; set; }
        [Index(1)]
        public virtual decimal BidPrice { get; set; }
        [Index(2)]
        public virtual decimal AskPrice { get; set; }
        [Index(3)]
        public virtual decimal LastPrice { get; set; }

        public ITicker ToTicker()
        {
            return new Ticker
            {
                Pair = Pair,
                BidPrice = BidPrice,
                AskPrice = AskPrice,
                LastPrice = LastPrice
            };
        }

        public static TickerData FromTicker(ITicker ticker)
        {
            return new TickerData
            {
                Pair = ticker.Pair,
                BidPrice = ticker.BidPrice,
                AskPrice = ticker.AskPrice,
                LastPrice = ticker.LastPrice
            };
        }
    }
}
