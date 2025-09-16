using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Levels.Enums;
using TgJobAdAnalytics.Models.Reports;


namespace TgJobAdAnalytics.Services.Analytics;

public sealed class SalaryStatisticsCalculator
{
    public static ReportGroup GenerateAll(List<SalaryEntity> salaries)
    {
        var filteredSalaries = RemoveOutliers(salaries);

        var reports = new List<Report>
        {
            GetMinimumByYearWithVariants(filteredSalaries),
            GetMaximumByYearWithVariants(filteredSalaries),
            GetAverageByYearWithVariants(filteredSalaries),
            GetMedianByYearWithVariants(filteredSalaries)
        };

        return new ReportGroup("Статистика по зарплатам", reports);
    }


    private static Report GetMinimumByYearWithVariants(List<SalaryEntity> salaries)
    {
        var baseResults = salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MinimalSalary = group.Select(group => group.LowerBoundNormalized).Minimum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MinimalSalary);

        var variants = new Dictionary<string, Dictionary<string, double>>
        {
            ["Все"] = baseResults
        };

        foreach (var level in Enum.GetValues<PositionLevel>().Where(l => l != PositionLevel.Unknown))
        {
            var perLevel = salaries
                .Where(s => s.Level == level && Math.Abs(s.LowerBoundNormalized) > Tolerance)
                .GroupBy(salary => salary.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    MinimalSalary = group.Select(x => x.LowerBoundNormalized).Minimum()
                })
                .OrderBy(group => group.Year)
                .ToDictionary(group => group.Year.ToString(), group => group.MinimalSalary);

            if (perLevel.Count > 0)
                variants[level.ToString()] = perLevel;
        }

        return new Report(
            title: "Минимальная зарплата по годам",
            results: baseResults,
            variants: variants
        );
    }


    private static Report GetMaximumByYearWithVariants(List<SalaryEntity> salaries)
    {
        var baseResults = salaries
            .Where(salary => Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MaximumSalary = group.Select(group => group.UpperBoundNormalized).Maximum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MaximumSalary);

        var variants = new Dictionary<string, Dictionary<string, double>>
        {
            ["Все"] = baseResults
        };

        foreach (var level in Enum.GetValues<PositionLevel>().Where(l => l != PositionLevel.Unknown))
        {
            var perLevel = salaries
                .Where(s => s.Level == level && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(salary => salary.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    MaximumSalary = group.Select(x => x.UpperBoundNormalized).Maximum()
                })
                .OrderBy(group => group.Year)
                .ToDictionary(group => group.Year.ToString(), group => group.MaximumSalary);

            if (perLevel.Count > 0)
                variants[level.ToString()] = perLevel;
        }

        return new Report(
            title: "Максимальная зарплата по годам",
            results: baseResults,
            variants: variants
        );
    }


    private static Report GetAverageByYearWithVariants(List<SalaryEntity> salaries)
    {
        var baseResults = salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance && Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MeanSalary = group.Select(GetNormalizedSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Mean()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MeanSalary);

        var variants = new Dictionary<string, Dictionary<string, double>>
        {
            ["Все"] = baseResults
        };

        foreach (var level in Enum.GetValues<PositionLevel>().Where(l => l != PositionLevel.Unknown))
        {
            var perLevel = salaries
                .Where(s => s.Level == level && Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(salary => salary.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    MeanSalary = group.Select(GetNormalizedSalaryValue)
                        .Where(salary => !double.IsNaN(salary))
                        .Mean()
                })
                .OrderBy(group => group.Year)
                .ToDictionary(group => group.Year.ToString(), group => group.MeanSalary);

            if (perLevel.Count > 0)
                variants[level.ToString()] = perLevel;
        }

        return new Report(
            title: "Средняя зарплата по годам",
            results: baseResults,
            variants: variants
        );
    }


    private static Report GetMedianByYearWithVariants(List<SalaryEntity> salaries)
    {
        var baseResults = salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance && Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MedianSalary = group.Select(GetNormalizedSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Median()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MedianSalary);

        var variants = new Dictionary<string, Dictionary<string, double>>();
        variants["Все"] = baseResults;

        var knownLevels = Enum.GetValues<PositionLevel>()
            .Where(l => l != PositionLevel.Unknown)
            .ToArray();

        foreach (var level in knownLevels)
        {
            var perLevel = salaries
                .Where(s => s.Level == level && Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(salary => salary.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    MedianSalary = group.Select(GetNormalizedSalaryValue)
                        .Where(salary => !double.IsNaN(salary))
                        .DefaultIfEmpty(double.NaN)
                        .Median()
                })
                .OrderBy(group => group.Year)
                .Where(g => !double.IsNaN(g.MedianSalary))
                .ToDictionary(group => group.Year.ToString(), group => group.MedianSalary);

            if (perLevel.Count > 0)
                variants[level.ToString()] = perLevel;
        }

        return new Report(
            title: "Медианная зарплата по годам",
            results: baseResults,
            variants: variants
        );
    }


    private static double GetNormalizedSalaryValue(SalaryEntity salary)
    {
        // No salary information
        if (double.IsNaN(salary.LowerBoundNormalized) && double.IsNaN(salary.UpperBoundNormalized))
            return double.NaN;

        if (double.IsNaN(salary.LowerBoundNormalized))
            return salary.UpperBoundNormalized;

        if (double.IsNaN(salary.UpperBoundNormalized))
            return salary.LowerBoundNormalized;

        return (salary.LowerBoundNormalized + salary.UpperBoundNormalized) / 2;
    }


    private static List<SalaryEntity> RemoveOutliers(List<SalaryEntity> salaries)
    {
        var excludedIds = GetExcludedIds(salaries, salary => salary.LowerBoundNormalized)
            .Concat(GetExcludedIds(salaries, salary => salary.UpperBoundNormalized))
            .ToHashSet();

        return salaries
            .Where(salary => !excludedIds.Contains(salary.Id))
            .ToList();


        IEnumerable<Guid> GetExcludedIds(List<SalaryEntity> salaries, Func<SalaryEntity, double> salarySelector)
        {
            var validLogValues = salaries
                .Select(salarySelector)
                .Where(salary => !double.IsNaN(salary) && Math.Abs(salary) > Tolerance)
                .Select(salary => Math.Log(salary))
                .ToArray();

            var (lowerThreshold, upperThreshold) = GetThresholds(validLogValues);

            return salaries.Where(message => IsOutlier(salarySelector(message), lowerThreshold, upperThreshold))
                .Select(salary => salary.Id);
        }


        static (double, double) GetThresholds(double[] validLogLowerBounds)
        {
            var q1 = validLogLowerBounds.Quantile(0.25);
            var q3 = validLogLowerBounds.Quantile(0.75);
            var iqr = q3 - q1;

            var lowerThreshold = q1 - 1.5 * iqr;
            var upperThreshold = q3 + 1.5 * iqr;

            return (lowerThreshold, upperThreshold);
        }


        static bool IsOutlier(double salary, double lowerThreshold, double upperThreshold)
        {
            if (double.IsNaN(salary))
                return false;

            var logSalary = Math.Log(salary);
            return logSalary <= lowerThreshold || upperThreshold <= logSalary;
        }
    }


    private const double Tolerance = 1e-10;
}
