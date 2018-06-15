using IntelliTrader.Core;
using IntelliTrader.Signals.Base;
using ZeroFormatter;

namespace IntelliTrader.Backtesting
{
    [ZeroFormattable]
    public class SignalData : ISignal
    {
        [Index(0)]
        public virtual string Name { get; set; }
        [Index(1)]
        public virtual string Pair { get; set; }
        [Index(2)]
        public virtual long? Volume { get; set; }
        [Index(3)]
        public virtual double? VolumeChange { get; set; }
        [Index(4)]
        public virtual decimal? Price { get; set; }
        [Index(5)]
        public virtual decimal? PriceChange { get; set; }
        [Index(6)]
        public virtual double? Rating { get; set; }
        [Index(7)]
        public virtual double? RatingChange { get; set; }
        [Index(8)]
        public virtual double? Volatility { get; set; }

        public ISignal ToSignal()
        {
            return new Signal
            {
                Name = Name,
                Pair = Pair,
                Volume = Volume,
                VolumeChange = VolumeChange,
                Price = Price,
                PriceChange = PriceChange,
                Rating = Rating,
                RatingChange = RatingChange,
                Volatility = Volatility
            };
        }

        public static SignalData FromSignal(ISignal signal)
        {
            return new SignalData
            {
                Name = signal.Name,
                Pair = signal.Pair,
                Volume = signal.Volume,
                VolumeChange = signal.VolumeChange,
                Price = signal.Price,
                PriceChange = signal.PriceChange,
                Rating = signal.Rating,
                RatingChange = signal.RatingChange,
                Volatility = signal.Volatility
            };
        }
    }
}
