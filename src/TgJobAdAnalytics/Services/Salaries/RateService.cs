﻿using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public class RateService : IRateService
{
    public RateService(Dictionary<(Currency, DateOnly), Rate> rates)
    {
        _rates = rates;
    }


    public double GetRate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate)
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
