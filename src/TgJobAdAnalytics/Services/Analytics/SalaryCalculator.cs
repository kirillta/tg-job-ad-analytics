using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Reports;


namespace TgJobAdAnalytics.Services.Analytics;

internal class SalaryCalculator
{
    public static ReportGroup CalculateAll(List<SalaryEntity> salaries)
    {
        var filteredSalaries = FilterOutliers(salaries);

        var reports = new List<Report>
        {
            GetMinimalByYear(filteredSalaries),
            GetMaximumByYear(filteredSalaries),
            GetMeanByYear(filteredSalaries),
            GetMedianByYear(filteredSalaries)
        };

        return new ReportGroup("Статистика по зарплатам", reports);
    }


    private static Report GetMinimalByYear(List<SalaryEntity> salaries)
        => salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MinimalSalary = group.Select(group => group.LowerBoundNormalized).Minimum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MinimalSalary)
            .ToReport("Минимальная зарплата по годам");


    private static Report GetMaximumByYear(List<SalaryEntity> salaries)
        => salaries
            .Where(salary => Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MaximumSalary = group.Select(group => group.UpperBoundNormalized).Maximum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MaximumSalary)
            .ToReport("Максимальная зарплата по годам");


    private static Report GetMeanByYear(List<SalaryEntity> salaries)
        => salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance && Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MeanSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Mean()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MeanSalary)
            .ToReport("Средняя зарплата по годам");


    private static Report GetMedianByYear(List<SalaryEntity> salaries)
        => salaries
            .Where(salary => Math.Abs(salary.LowerBoundNormalized) > Tolerance && Math.Abs(salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MedianSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Median()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MedianSalary)
            .ToReport("Медианная зарплата по годам");


    private static double GetSalaryValue(SalaryEntity salary)
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


    private static List<SalaryEntity> FilterOutliers(List<SalaryEntity> salaries)
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
