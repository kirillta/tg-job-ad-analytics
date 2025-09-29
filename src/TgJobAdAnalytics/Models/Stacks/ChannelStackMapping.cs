using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Stacks;

/// <summary>
/// Root model for channel/chat to stack mapping file.
/// </summary>
public sealed class ChannelStackMapping
{
    /// <summary>
    /// Gets or sets the channel/chat entries.
    /// </summary>
    [JsonPropertyName("channels")]
    public List<ChannelStackEntry> Channels { get; set; } = [];
}
