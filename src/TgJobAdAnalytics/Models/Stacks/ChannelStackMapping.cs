using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Stacks;

public sealed class ChannelStackMapping
{
    [JsonPropertyName("channels")]
    public List<ChannelStackEntry> Channels { get; set; } = [];
}
