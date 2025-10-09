using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Models.Salaries;

/// <summary>
/// Result of salary text processing / extraction. Contains original numeric bounds and currency as parsed from the source
/// along with normalized currency and converted bounds (e.g. converted into a canonical reporting currency).
/// Bounds are inclusive and expressed as gross amounts when determinable; when only a single value is parsed it populates both bounds.
/// </summary>
public readonly record struct SalaryProcessingResult
{
    /// <summary>
    /// Gets the lower bound of the detected salary range in the original currency.
    /// </summary>
    public double LowerBound { get; init; }

    /// <summary>
    /// Gets the upper bound of the detected salary range in the original currency.
    /// </summary>
    public double UpperBound { get; init; }

    /// <summary>
    /// Gets the currency as detected in the source text. <see cref="Currency.Unknown"/> when not identifiable.
    /// </summary>
    public Currency Currency { get; init; }

    /// <summary>
    /// Gets the normalized currency chosen for analytics / aggregation (e.g. a canonical base currency).
    /// </summary>
    public Currency CurrencyNormalized { get; init; }

    /// <summary>
    /// Gets the lower bound converted into the normalized currency.
    /// </summary>
    public double LowerBoundNormalized { get; init; }

    /// <summary>
    /// Gets the upper bound converted into the normalized currency.
    /// </summary>
    public double UpperBoundNormalized { get; init; }
}
