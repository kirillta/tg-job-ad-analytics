using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Stacks;

public sealed class ChannelStackEntry
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("stack")]
    public string Stack { get; set; } = string.Empty;
}
