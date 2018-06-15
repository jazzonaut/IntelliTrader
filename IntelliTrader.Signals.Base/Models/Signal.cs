using IntelliTrader.Core;

namespace IntelliTrader.Signals.Base
{
    public class Signal : ISignal
    {
        public string Name { get; set; }
        public string Pair { get; set; }
        public long? Volume { get; set; }
        public double? VolumeChange { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceChange { get; set; }
        public double? Rating { get; set; }
        public double? RatingChange { get; set; }
        public double? Volatility { get; set; }
    }
}
