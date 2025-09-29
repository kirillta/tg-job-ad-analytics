using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Stacks;

/// <summary>
/// One mapping entry. Compare by chat id. Channel name is for readability only.
/// </summary>
public sealed class ChannelStackEntry
{
    /// <summary>
    /// Telegram chat id used for matching.
    /// </summary>
    [JsonPropertyName("chatId")]
    public long ChatId { get; set; }

    /// <summary>
    /// Channel name for readability only.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Canonical stack name.
    /// </summary>
    [JsonPropertyName("stack")]
    public string StackName { get; set; } = string.Empty;
}
