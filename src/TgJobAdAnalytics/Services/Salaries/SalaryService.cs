using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public class SalaryService
{
    public SalaryService(Currency baseCurrency, RateService rateService)
    {
        _baseCyrrency = baseCurrency;
        _rateService = rateService;
    }


    public ValueTask<Salary> Get(string text, DateOnly date)
    {
        var salary = ParseBoundaries(text);
        return Normalize(date, salary);
    }


    private async ValueTask<Salary> Normalize(DateOnly date, Salary salary)
    {
        if (salary.Currency == Currency.Unknown || salary.Currency == _baseCyrrency)
            return salary with { LowerBoundNormalized = salary.LowerBound, UpperBoundNormalized = salary.UpperBound };

        var lowerNormalized = await NormalizeInternal(salary.LowerBound);
        var upperNormalized = await NormalizeInternal(salary.UpperBound);

        return salary with { LowerBoundNormalized = lowerNormalized, UpperBoundNormalized = upperNormalized };


        async ValueTask<double> NormalizeInternal(double value)
        {
            if (double.IsNaN(value) || value == 0)
                return value;

            var rate = await _rateService.GetRate(_baseCyrrency, salary.Currency, date);
            var amount = value * rate;

            return Math.Round(amount, 4);
        }
    }


    private static Salary ParseBoundaries(string text)
    {
        var salaryPatterns = SalaryPattenrFactory.Get();
        foreach (var pattern in salaryPatterns)
        {
            var match = pattern.Regex.Match(text);
            if (match.Success)
            {
                var lowerBound = ParseSalary(match.Groups[1].Value);
                var upperBound = ParseSalary(match.Groups[2].Value);

                return new Salary(lowerBound, upperBound, pattern.Currency);
            }
        }

        return new Salary(double.NaN, double.NaN, Currency.Unknown);


        static double ParseSalary(string salaryString)
        {
            salaryString = salaryString.Replace("k", "000", StringComparison.OrdinalIgnoreCase)
                .Replace("тр", "000", StringComparison.OrdinalIgnoreCase);

            return double.TryParse(salaryString, out var result) ? result : double.NaN;
        }
    }


    private readonly Currency _baseCyrrency;
    private readonly RateService _rateService;
}
