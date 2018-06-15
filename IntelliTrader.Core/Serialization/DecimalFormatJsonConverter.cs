using Newtonsoft.Json;
using System;

namespace IntelliTrader.Core
{
    public class DecimalFormatJsonConverter : JsonConverter
    {
        private readonly int _numberOfDecimals;

        public DecimalFormatJsonConverter(int numberOfDecimals)
        {
            _numberOfDecimals = numberOfDecimals;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var d = (decimal)value;
            var rounded = Math.Round(d, _numberOfDecimals, MidpointRounding.AwayFromZero);
            writer.WriteValue(rounded);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }
    }
}
