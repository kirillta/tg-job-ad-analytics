using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Provides a factory for creating and managing instances of RateService using a specified API client and rate source
/// manager.
/// </summary>
/// <remarks>RateServiceFactory is designed to ensure that only one RateService instance is created per factory
/// instance. It coordinates retrieval and caching of currency rate data from external sources and local storage. This
/// class is not thread-safe; concurrent calls to Create may result in multiple RateService instances being
/// created.</remarks>
public sealed class RateServiceFactory
{
    /// <summary>
    /// Initializes a new instance of the RateServiceFactory class using the specified API client and rate source
    /// manager.
    /// </summary>
    /// <param name="rateApiClient">The RateApiClient instance used to communicate with the external rate API. Cannot be null.</param>
    /// <param name="rateSourceManager">The RateSourceManager instance that manages available rate sources. Cannot be null.</param>
    public RateServiceFactory(RateApiClient rateApiClient, RateSourceManager rateSourceManager)
    {
        _rateApiClient = rateApiClient;
        _rateSourceManager = rateSourceManager;

        _rate = null;
    }


    /// <summary>
    /// Asynchronously creates and returns a new instance of the rate service for the specified base currency and
    /// initial date, or returns an existing instance if one has already been created.
    /// </summary>
    /// <param name="baseCurrency">The currency to use as the base for rate calculations. This determines which currency rates will be referenced.</param>
    /// <param name="initialDate">The initial date for which the rate service should be initialized. This sets the starting point for rate data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the rate service instance for the
    /// specified base currency and initial date.</returns>
    public async ValueTask<RateService> Create(Currency baseCurrency, DateOnly initialDate, CancellationToken cancellationToken)
    {
        if (_rate is not null)
            return _rate;

        _rate = await GetRateService(baseCurrency, initialDate, cancellationToken);
        return _rate;
    }


    private async ValueTask<RateService> GetRateService(Currency baseCurrency, DateOnly initialDate, CancellationToken cancellationToken)
    {
        var rates = _rateSourceManager.Get();

        var searchDate = initialDate;
        var storedFinalDate = _rateSourceManager.GetMaximalDate();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (storedFinalDate < today)
            searchDate = storedFinalDate;

        if (_rateSourceManager.GetMinimalDate() <= searchDate && searchDate == today)
            return new RateService(rates);

        foreach (Currency currency in GetTargetCurrencies(baseCurrency))
        {
            var apiRates = await _rateApiClient.Get(baseCurrency, currency, searchDate, cancellationToken);
            await _rateSourceManager.Add(apiRates);
        }

        rates = _rateSourceManager.Get();
        return new RateService(rates);
    }


    private static List<Currency> GetTargetCurrencies(Currency baseCurrency)
    {
        var currencies = new List<Currency>();
        foreach (Currency currency in Enum.GetValues<Currency>())
        {
            if (currency != baseCurrency && currency is not Currency.Unknown)
                currencies.Add(currency);
        }

        return currencies;
    }


    private RateService? _rate;

    private readonly RateApiClient _rateApiClient;
    private readonly RateSourceManager _rateSourceManager;
}
