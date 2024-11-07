using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct Salary
{
    [JsonConstructor]
    public Salary(double lowerBound, double upperBound, Currency currency)
    {
        Currency = currency;
        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public Currency Currency { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
}
