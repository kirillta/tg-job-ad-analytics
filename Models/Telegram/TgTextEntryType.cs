using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Telegram.Converters;

namespace TgJobAdAnalytics.Models.Telegram;

[JsonConverter(typeof(TgTextEntryTypeConverter))]
public enum TgTextEntryType
{
    Email,
    HashTag,
    Link,
    PlainText,
    NonValueble
}
