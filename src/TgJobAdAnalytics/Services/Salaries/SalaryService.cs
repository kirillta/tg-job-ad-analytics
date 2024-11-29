using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public class SalaryService
{
    public SalaryService(Currency baseCurrency, RateService rateService)
    {
        _baseCyrrency = baseCurrency;
        _rateService = rateService;
    }


    public Salary Get(string text, DateOnly date)
    {
        var salary = ParseBoundaries(text);
        return Normalize(date, salary);
    }


    private Salary Normalize(DateOnly date, Salary salary)
    {
        if (salary.Currency == Currency.Unknown || salary.Currency == _baseCyrrency)
            return salary with { LowerBoundNormalized = salary.LowerBound, UpperBoundNormalized = salary.UpperBound };

        var lowerNormalized = NormalizeInternal(salary.LowerBound);
        var upperNormalized = NormalizeInternal(salary.UpperBound);

        return salary with { LowerBoundNormalized = lowerNormalized, UpperBoundNormalized = upperNormalized };


        double NormalizeInternal(double value)
        {
            if (double.IsNaN(value) || value == 0)
                return value;

            var rate = _rateService.GetRate(_baseCyrrency, salary.Currency, date);
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
                var lowerBound = double.NaN;
                var upperBound = double.NaN;

                switch (pattern.BoundaryType)
                {
                    case BoundaryType.Both:
                        lowerBound = ParseSalary(match.Groups[1].Value);
                        upperBound = ParseSalary(match.Groups[2].Value);
                        break;
                    case BoundaryType.Lower:
                        lowerBound = ParseSalary(match.Groups[1].Value);
                        break;
                    case BoundaryType.Upper:
                        lowerBound = ParseSalary(match.Groups[1].Value);
                        break;
                }
                
                return new Salary(lowerBound, upperBound, pattern.Currency);
            }
        }

        return new Salary(double.NaN, double.NaN, Currency.Unknown);


        static double ParseSalary(string salaryString)
        {
            salaryString = salaryString.Replace("k", "000", StringComparison.OrdinalIgnoreCase)
                .Replace("к", "000", StringComparison.OrdinalIgnoreCase)
                .Replace("тр", "000", StringComparison.OrdinalIgnoreCase);

            return double.TryParse(salaryString, out var result) ? result : double.NaN;
        }
    }


    private readonly Currency _baseCyrrency;
    private readonly RateService _rateService;
}
