using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public class RateApiClient
{
    public async ValueTask<double?> Get(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate)
    {
        return await Task.FromResult<double?>(new Random().NextDouble());
    }
}
