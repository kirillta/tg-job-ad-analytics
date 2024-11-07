using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryNormalizer
{
    public SalaryNormalizer(Currency targetCurrancy)
    {
        _targetCurrancy = targetCurrancy;
    }


    public Salary Normalize(Salary salary)
    {
        if (salary.Currency == _targetCurrancy)
            return salary;

        var normalizedLower = NormalizeInternal(salary.LowerBound);
        var normalizedUpper = NormalizeInternal(salary.UpperBound);

        return new Salary(normalizedLower, normalizedUpper, _targetCurrancy);


        double NormalizeInternal(double value)
        {
            if (double.IsNaN(value))
                return double.NaN;

            var normalized = value * GetRate(salary.Currency);
            return normalized;
        }

        double GetRate(Currency currency)
        {
            return currency switch
            {
                Currency.Unknown => double.NaN,
                Currency.USD => 1.0,
                Currency.EUR => 0.85,
                Currency.RUB => 75.0,
                _ => throw new ArgumentException($"Unknown currency: {currency}")
            };
        }
    }


    private readonly Currency _targetCurrancy;
}
