using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Models.Salaries;

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


    public static Salary Empty
        => new(double.NaN, double.NaN, Currency.Unknown);


    public Currency Currency { get; init; }
    public double LowerBound { get; init; }
    public double LowerBoundNormalized { get; init; }
    public double UpperBound { get; init; }
    public double UpperBoundNormalized { get; init; }
}
