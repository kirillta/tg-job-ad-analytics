using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct SalaryProcessingResult
{
    public double LowerBound { get; init; }

    public double UpperBound { get; init; }

    public Currency Currency { get; init; }

    public Currency CurrencyNormalized { get; init; }

    public double LowerBoundNormalized { get; init; }

    public double UpperBoundNormalized { get; init; }
}
