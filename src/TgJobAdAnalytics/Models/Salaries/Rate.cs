using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Models.Salaries;

/// <summary>
/// Represents a currency exchange rate between a base currency and a target currency valid on a specific date.
/// Immutable value object used for salary normalization and historical analytics.
/// </summary>
public readonly record struct Rate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Rate"/> record struct.
    /// </summary>
    /// <param name="baseCurrency">The currency from which conversion starts.</param>
    /// <param name="targetCurrency">The currency to which conversion is made.</param>
    /// <param name="targetDate">The date for which this rate is valid (usually market close or publishing date).</param>
    /// <param name="value">The numeric conversion factor (1 baseCurrency = value targetCurrency).</param>
    public Rate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate, double value)
    {
        BaseCurrency = baseCurrency;
        TargetCurrency = targetCurrency;
        TargetDate = targetDate;
        Value = value;
    }

    /// <summary>
    /// Gets the base currency of the rate.
    /// </summary>
    public Currency BaseCurrency { get; }

    /// <summary>
    /// Gets the target currency of the rate.
    /// </summary>
    public Currency TargetCurrency { get; }

    /// <summary>
    /// Gets the date the rate applies to.
    /// </summary>
    public DateOnly TargetDate { get; }

    /// <summary>
    /// Gets the conversion factor where 1 unit of <see cref="BaseCurrency"/> equals this many units of <see cref="TargetCurrency"/>.
    /// </summary>
    public double Value { get; }
}
