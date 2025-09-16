using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct Rate
{
    public Rate(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate, double value)
    {
        BaseCurrency = baseCurrency;
        TargetCurrency = targetCurrency;
        TargetDate = targetDate;
        Value = value;
    }


    public Currency BaseCurrency { get; }

    public Currency TargetCurrency { get; }

    public DateOnly TargetDate { get; }

    public double Value { get; }
}
