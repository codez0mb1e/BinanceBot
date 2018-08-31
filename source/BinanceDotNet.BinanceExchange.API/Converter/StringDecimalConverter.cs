using System;
using System.Globalization;
using Newtonsoft.Json;

namespace BinanceExchange.API.Converter
{
    public class StringDecimalConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal) || objectType == typeof(decimal?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((decimal) value).ToString("F6", CultureInfo.InvariantCulture));
        }
    }
}