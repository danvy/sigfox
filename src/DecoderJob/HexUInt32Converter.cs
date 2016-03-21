using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DecoderJob
{
    public class HexUInt32Converter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((UInt32)value).ToString("X"));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;
            return UInt32.Parse((string)reader.Value, System.Globalization.NumberStyles.HexNumber);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}