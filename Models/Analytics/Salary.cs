using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct Salary
{
    [JsonConstructor]
    public Salary(double lowerBound, double lowerBoundNormalized, double upperBound, double upperBoundNormalized, Currency currency)
    {
        Currency = currency;
        LowerBound = lowerBound;
        LowerBoundNormalized = lowerBoundNormalized;
        UpperBound = upperBound;
        UpperBoundNormalized = upperBoundNormalized;
    }


    public Salary(double lowerBound, double upperBound, Currency currency)
        : this(lowerBound, double.NaN, upperBound, double.NaN, currency) 
    { }


    public Currency Currency { get; init; }
    public double LowerBound { get; init; }
    public double LowerBoundNormalized { get; init; }
    public double UpperBound { get; init; }
    public double UpperBoundNormalized { get; init; }
}
