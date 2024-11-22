using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Telegram;

public readonly record struct TgMessage
{
    public TgMessage(long id, DateTime date, List<TgTextEntry> textEntities)
    {
        Id = id;
        Date = date;
        TextEntities = textEntities;
    }


    [JsonPropertyName("id")]
    public long Id { get; init; }
    [JsonPropertyName("date")]
    public DateTime Date { get; init; }
    [JsonPropertyName("text_entities")]
    public List<TgTextEntry> TextEntities { get; init; }
}
