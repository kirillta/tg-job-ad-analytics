using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Telegram.Enums;

namespace TgJobAdAnalytics.Models.Telegram;

public readonly record struct TgTextEntry
{
    [JsonConstructor]
    public TgTextEntry(string text, TgTextEntryType type)
    {
        Text = text;
        Type = type;
    }


    [JsonPropertyName("text")]
    public string Text { get; init; }

    [JsonPropertyName("type")]
    public TgTextEntryType Type { get; init; }
}
