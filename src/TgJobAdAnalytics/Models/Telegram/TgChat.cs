using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Telegram;

public readonly record struct TgChat
{
    [JsonConstructor]
    public TgChat(long id, string name, List<TgMessage> messages)
    {
        Id = id;
        Name = name;
        Messages = messages;
    }


    [JsonPropertyName("id")]
    public long Id { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
    [JsonPropertyName("messages")]
    public List<TgMessage> Messages { get; init; }
}
