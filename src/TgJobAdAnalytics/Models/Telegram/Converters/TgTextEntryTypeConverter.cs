using System.Text.Json;
using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Telegram.Converters;

public class TgTextEntryTypeConverter : JsonConverter<TgTextEntryType>
{
    public override TgTextEntryType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "email" => TgTextEntryType.Email,
            "hashtag" => TgTextEntryType.HashTag,
            "link" => TgTextEntryType.Link,
            "plain" => TgTextEntryType.PlainText,
            _ => TgTextEntryType.NonValueble
        };
    }


    public override void Write(Utf8JsonWriter writer, TgTextEntryType value, JsonSerializerOptions options)
    {
        var jsonValue = value switch
        {
            TgTextEntryType.Email => "email",
            TgTextEntryType.HashTag => "hashtag",
            TgTextEntryType.Link => "link",
            TgTextEntryType.PlainText => "plain",
            TgTextEntryType.NonValueble => "non_valueble",
            _ => throw new JsonException($"Unknown value: {value}")
        };

        writer.WriteStringValue(jsonValue);
    }
}
