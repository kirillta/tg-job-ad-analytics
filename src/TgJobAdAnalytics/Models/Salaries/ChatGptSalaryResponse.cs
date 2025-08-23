using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct ChatGptSalaryResponse
{
    [JsonConstructor]
    public ChatGptSalaryResponse(bool isSalaryPresent, double? lowerBound, double? upperBound, Currency? currency, Period? period)
    {
        IsSalaryPresent = isSalaryPresent;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Currency = currency;
        Period = period;
    }


    [JsonPropertyName("p")]
    public bool IsSalaryPresent { get; init; }

    [JsonPropertyName("lb")]
    public double? LowerBound { get; init; }

    [JsonPropertyName("ub")]
    public double? UpperBound { get; init; }

    [JsonPropertyName("prd")]
    public Period? Period { get; init; }

    [JsonPropertyName("cur")]
    public Currency? Currency { get; }
}
