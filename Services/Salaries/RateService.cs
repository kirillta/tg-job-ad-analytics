using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public class RateService
{
    public RateService(RateSourceManager rateSourceManager, RateApiClient rateApiClient)
    {
        _rateApiClient = rateApiClient;
        _rateSourceManager = rateSourceManager;
    }
    

    public async ValueTask<double> GetRate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate)
    {
        if (baseCurrency == targetCurrency)
            return 1.0;

        var rate = await TryGetStoredRate(baseCurrency, targetCurrency, targetDate);
        if (rate is not null)
            return rate.Value;

        rate = await _rateApiClient.Get(baseCurrency, targetCurrency, targetDate);
        if (rate is null)
            throw new Exception("Failed to get rate from API");

        await _rateSourceManager.Add(baseCurrency, targetCurrency, targetDate, rate.Value);
        return rate.Value;
    }


    private async ValueTask<double?> TryGetStoredRate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate)
    {
        var rates = _rateSourceManager.Get();
        if (rates.TryGetValue((baseCurrency, targetCurrency, targetDate), out var rate))
            return rate;

        if (!rates.TryGetValue((targetCurrency, baseCurrency, targetDate), out rate))
            return null;

        rate = 1.0 / rate;
        await _rateSourceManager.Add(baseCurrency, targetCurrency, targetDate, rate);

        return rate;
    }


    private readonly RateApiClient _rateApiClient;
    private readonly RateSourceManager _rateSourceManager;
}
