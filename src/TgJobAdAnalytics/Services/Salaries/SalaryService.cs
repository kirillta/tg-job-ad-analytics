using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public class SalaryService
{
    public SalaryService(Currency baseCurrency, IRateService rateService)
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
                        var isThousandBoth = ContainsThousandSymbols(match.Groups[1].Value) || ContainsThousandSymbols(match.Groups[2].Value);

                        lowerBound = ParseSalary(isThousandBoth, match.Groups[1].Value);
                        upperBound = ParseSalary(isThousandBoth, match.Groups[2].Value);
                        break;
                    case BoundaryType.Lower:
                        var isThousandLower = ContainsThousandSymbols(match.Groups[1].Value);
                        lowerBound = ParseSalary(isThousandLower, match.Groups[1].Value);
                        break;
                    case BoundaryType.Upper:
                        var isThousandUpper = ContainsThousandSymbols(match.Groups[1].Value);
                        lowerBound = ParseSalary(isThousandUpper, match.Groups[1].Value);
                        break;
                }

                return new Salary(lowerBound, upperBound, pattern.Currency);
            }
        }

        return new Salary(double.NaN, double.NaN, Currency.Unknown);


        static bool ContainsThousandSymbols(string salaryString)
        {
            foreach (var symbol in ThousandSymbols)
            {
                if (salaryString.Contains(symbol, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        static string RemoveThousandSymbols(string salaryString)
        {
            foreach (var symbol in ThousandSymbols)
            {
                salaryString = salaryString.Replace(symbol, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return salaryString;
        }


        static double ParseSalary(bool isThousand, string salaryString)
        {
            salaryString = RemoveThousandSymbols(salaryString);
            var salary = double.TryParse(salaryString, out var result) ? result : double.NaN;

            if (isThousand)
                salary *= 1000;

            return salary;
        }
    }


    private static readonly List<string> ThousandSymbols = ["k", "к", "тр", "тыср"];

    private readonly Currency _baseCyrrency;
    private readonly IRateService _rateService;
}
