using System.Text.Json;
using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Telegram.Enums;

namespace TgJobAdAnalytics.Models.Telegram.Converters;

public class TgTextEntryTypeConverter : JsonConverter<TgTextEntryType>
{
    public override TgTextEntryType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "bold" => TgTextEntryType.Bold,
            "code" => TgTextEntryType.Code,
            "custom_emoji" => TgTextEntryType.CustomEmoji,
            "email" => TgTextEntryType.Email,
            "hashtag" => TgTextEntryType.HashTag,
            "italic" => TgTextEntryType.Italic,
            "link" => TgTextEntryType.Link,
            "plain" => TgTextEntryType.PlainText,
            "pre" => TgTextEntryType.Pre,
            "strikethrough" => TgTextEntryType.Strikethrough,
            "underline" => TgTextEntryType.Underline,
            _ => TgTextEntryType.NonValueble
        };
    }


    public override void Write(Utf8JsonWriter writer, TgTextEntryType value, JsonSerializerOptions options)
    {
        var jsonValue = value switch
        {
            TgTextEntryType.Bold => "bold",
            TgTextEntryType.Code => "code",
            TgTextEntryType.CustomEmoji => "custom_emoji",
            TgTextEntryType.Email => "email",
            TgTextEntryType.HashTag => "hashtag",
            TgTextEntryType.Italic => "italic",
            TgTextEntryType.Link => "link",
            TgTextEntryType.PlainText => "plain",
            TgTextEntryType.Pre => "pre",
            TgTextEntryType.Strikethrough => "strikethrough",
            TgTextEntryType.Underline => "underline",
            TgTextEntryType.NonValueble => "non_valueble",
            _ => throw new JsonException($"Unknown value: {value}")
        };

        writer.WriteStringValue(jsonValue);
    }
}
