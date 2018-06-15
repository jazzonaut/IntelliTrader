using IntelliTrader.Signals.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace IntelliTrader.Signals.TradingView
{
    internal class TradingViewCryptoSignalConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Signal);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var array = JArray.Load(reader);
                var item = (existingValue as Signal ?? new Signal());
                item.Pair = (string)array.ElementAtOrDefault(0);
                item.Price = (decimal?)array.ElementAtOrDefault(1);
                item.PriceChange = (decimal?)array.ElementAtOrDefault(2);
                item.Volume = (long?)array.ElementAtOrDefault(3);
                item.Rating = (double?)array.ElementAtOrDefault(4);
                item.Volatility = (double?)array.ElementAtOrDefault(5);
                return item;
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
