using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class RateServiceFactory
{
    public RateServiceFactory(string sourcePath)
    {
        _rateApiClient = new RateApiClient();
        _rateSourceManager = new RateSourceManager(Path.Combine(sourcePath, "rates.csv"));
    }


    public async ValueTask<RateService> Get(Currency baseCurrency, DateOnly initialDate)
    {
        var rates = _rateSourceManager.Get();

        var searchDate = initialDate;
        var storedFinalDate = _rateSourceManager.GetMaximalDate();
        if (storedFinalDate < DateOnly.FromDateTime(DateTime.Now))
            searchDate = storedFinalDate;

        if (_rateSourceManager.GetMinimalDate() <= searchDate)
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


    private readonly RateApiClient _rateApiClient;
    private readonly RateSourceManager _rateSourceManager;
}
