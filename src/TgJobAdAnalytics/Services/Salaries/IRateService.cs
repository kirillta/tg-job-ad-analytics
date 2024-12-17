using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries
{
    public interface IRateService
    {
        double GetRate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate);
    }
}