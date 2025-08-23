using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Telegram.Converters;

namespace TgJobAdAnalytics.Models.Telegram;

[JsonConverter(typeof(TgTextEntryTypeConverter))]
public enum TgTextEntryType
{
    Bold,
    Code,
    CustomEmoji,
    Email,
    HashTag,
    Italic,
    Link,
    PlainText,
    Pre,
    Strikethrough,
    Underline,
    NonValueble
}
