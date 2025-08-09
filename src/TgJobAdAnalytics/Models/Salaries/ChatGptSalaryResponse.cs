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


    [JsonPropertyName("salary_present")]
    public bool IsSalaryPresent { get; }

    [JsonPropertyName("lower_bound")]
    public double? LowerBound { get; }

    [JsonPropertyName("upper_bound")]
    public double? UpperBound { get; }

    [JsonPropertyName("currency")]
    public Currency? Currency { get; }

    [JsonPropertyName("period")]
    public Period? Period { get; }
}
