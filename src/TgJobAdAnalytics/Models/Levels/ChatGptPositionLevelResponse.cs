using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Levels.Enums;

namespace TgJobAdAnalytics.Models.Levels;

public readonly record struct ChatGptPositionLevelResponse
{
    [JsonConstructor]
    public ChatGptPositionLevelResponse(PositionLevel level)
    {
        Level = level;
    }


    public static ChatGptPositionLevelResponse Empty 
        => new(PositionLevel.Unknown);


    [JsonPropertyName("pl")]
    public PositionLevel Level { get; init; }
}
