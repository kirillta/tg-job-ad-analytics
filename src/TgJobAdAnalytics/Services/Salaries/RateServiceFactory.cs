using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class RateServiceFactory
{
    public RateServiceFactory(string sourcePath)
    {
        _rateApiClient = new RateApiClient();
        _rateSourceManager = new RateSourceManager(Path.Combine(sourcePath, "rates.csv"));

        _rate = null;
    }


    public async ValueTask<RateService> Get(Currency baseCurrency, DateOnly initialDate)
    {
        if (_rate is not null)
            return _rate;

        _rate = await GetRateService(baseCurrency, initialDate);
        return _rate;
    }


    private async ValueTask<RateService> GetRateService(Currency baseCurrency, DateOnly initialDate)
    {
        var rates = _rateSourceManager.Get();

        var searchDate = initialDate;
        var storedFinalDate = _rateSourceManager.GetMaximalDate();
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (storedFinalDate < today)
            searchDate = storedFinalDate;

        if (_rateSourceManager.GetMinimalDate() <= searchDate && searchDate == today)
            return new RateService(rates);

        foreach (Currency currency in GetTargetCurrencies(baseCurrency))
        {
            var apiRates = await _rateApiClient.Get(baseCurrency, currency, searchDate);
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
