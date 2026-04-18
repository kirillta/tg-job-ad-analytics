using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Levels.Enums;

namespace TgJobAdAnalytics.Models.Levels;

public readonly record struct ChatGptPositionLevelResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatGptPositionLevelResponse"/> record struct.
    /// </summary>
    [JsonConstructor]
    public ChatGptPositionLevelResponse(PositionLevel level)
    {
        Level = level;
    }


    /// <summary>
    /// Gets an empty response representing an unknown position level.
    /// </summary>
    public static ChatGptPositionLevelResponse Empty 
        => new(PositionLevel.Unknown);


    /// <summary>
    /// Gets the position level associated with the job advertisement.
    /// </summary>
    [JsonPropertyName("pl")]
    public PositionLevel Level { get; init; }
}
