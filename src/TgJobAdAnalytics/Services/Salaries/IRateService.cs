using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

public interface IRateService
{
    double Get(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate);
}