using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartCampus.API.Helpers
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return TimeSpan.Zero;
            
            // Try parsing as HH:mm:ss or HH:mm format
            if (TimeSpan.TryParse(value, out var timeSpan))
                return timeSpan;
            
            // Try parsing as HH:mm format
            var parts = value.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
                return new TimeSpan(hours, minutes, 0);
            
            return TimeSpan.Zero;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm\:ss")); // hh for 24-hour format with leading zero
        }
    }
}

