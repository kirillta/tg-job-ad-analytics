using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Defines a service for retrieving exchange rates between currencies for a specified date.
/// </summary>
/// <remarks>Implementations of this interface provide currency conversion rates, which may be based on market
/// data, central bank rates, or other sources. The returned rate typically represents the amount of the target currency
/// equivalent to one unit of the base currency on the given date. Thread safety and data freshness depend on the
/// specific implementation.</remarks>
public interface IRateService
{
    /// <summary>
    /// Retrieves the exchange rate from the specified base currency to the target currency on the given date.
    /// </summary>
    /// <param name="baseCurrency">The currency from which the exchange rate is calculated. Must be a valid currency code.</param>
    /// <param name="targetCurrency">The currency to which the exchange rate is calculated. Must be a valid currency code.</param>
    /// <param name="targetDate">The date for which the exchange rate is requested.</param>
    /// <returns>The exchange rate as a double value representing the amount of target currency per one unit of base currency on
    /// the specified date.</returns>
    double Get(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate);
}