using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Locations.Enums;

namespace TgJobAdAnalytics.Models.Locations;

/// <summary>
/// Deserialized response from the LLM location and work format extraction call.
/// </summary>
public readonly record struct ChatGptLocationFormatResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatGptLocationFormatResponse"/> record struct.
    /// </summary>
    [JsonConstructor]
    public ChatGptLocationFormatResponse(VacancyLocation location, WorkFormat format)
    {
        Location = location;
        Format = format;
    }


    /// <summary>
    /// Employer geographic location (0–7).
    /// </summary>
    [JsonPropertyName("loc")]
    public VacancyLocation Location { get; init; }

    /// <summary>
    /// Required work arrangement (0–4).
    /// </summary>
    [JsonPropertyName("fmt")]
    public WorkFormat Format { get; init; }
}
