using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrokenStatsBackend.src.Util
{
    public class DateTimeConverterUsingIso8601 : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("o")); // ISO 8601
        }
    }
}