using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public class SalaryService
{
    public SalaryService(SalaryNormalizer salaryNormalizer)
    {
        _salaryNormalizer = salaryNormalizer;
    }


    public Salary Get(string text)
    {
        var salary = GetSalaryBounds(text);
        var normalizedSalary = _salaryNormalizer.Normalize(salary);

        return normalizedSalary;
    }


    private static Salary GetSalaryBounds(string text)
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

        return default;


        static double ParseSalary(string salary)
        {
            salary = salary.Replace(" ", "").Replace(",", "").Replace("k", "000", StringComparison.OrdinalIgnoreCase);
            return double.TryParse(salary, out var result) ? result : double.NaN;
        }
    }


    private readonly SalaryNormalizer _salaryNormalizer;
}
