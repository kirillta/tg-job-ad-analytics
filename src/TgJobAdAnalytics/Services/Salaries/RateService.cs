using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// In-memory implementation of <see cref="IRateService"/> that returns preloaded exchange <see cref="Rate"/> values
/// keyed by (base currency, date). Assumes caller supplies consistent forward rates (and inverse if needed).
/// </summary>
public sealed class RateService : IRateService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateService"/>.
    /// </summary>
    /// <param name="rates">Dictionary keyed by (base currency, date) containing exchange rates.</param>
    public RateService(Dictionary<(Currency, DateOnly), Rate> rates)
    {
        _rates = rates;
    }


    /// <inheritdoc />
    public double Get(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate)
    {
        if (baseCurrency == targetCurrency)
            return 1.0;

        if (_rates.TryGetValue((targetCurrency, targetDate), out var rate))
        {
            if (rate == default)
                throw new Exception("Failed to get rate from API");
        }

        return rate.Value;
    }


    private readonly Dictionary<(Currency, DateOnly), Rate> _rates;
}
