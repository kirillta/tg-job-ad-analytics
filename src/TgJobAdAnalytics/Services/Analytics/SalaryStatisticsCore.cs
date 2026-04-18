using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Levels.Enums;

namespace TgJobAdAnalytics.Services.Analytics;

/// <summary>
/// Shared core salary statistics utilities (normalization, outliers, yearly aggregates).
/// </summary>
internal static class SalaryStatisticsCore
{
    internal static IReadOnlyList<SalaryEntity> RemoveOutliers(IReadOnlyList<SalaryEntity> salaries)
    {
        var excludedIds = GetExcludedIds(salaries, s => s.LowerBoundNormalized)
            .Concat(GetExcludedIds(salaries, s => s.UpperBoundNormalized))
            .ToHashSet();

        return [.. salaries.Where(s => !excludedIds.Contains(s.Id))];


        static IEnumerable<Guid> GetExcludedIds(IReadOnlyList<SalaryEntity> source, Func<SalaryEntity, double> selector)
        {
            var validLogValues = source
                .Select(s => selector(s))
                .Where(v => !double.IsNaN(v) && Math.Abs(v) > Tolerance)
                .Select(v => Math.Log(v))
                .ToArray();

            if (validLogValues.Length == 0)
                return [];

            var q1 = validLogValues.Quantile(0.25);
            var q3 = validLogValues.Quantile(0.75);
            var iqr = q3 - q1;
            var lowerThreshold = q1 - 1.5 * iqr;
            var upperThreshold = q3 + 1.5 * iqr;

            return source.Where(s => IsOutlier(selector(s), lowerThreshold, upperThreshold))
                .Select(s => s.Id);
        }

        static bool IsOutlier(double value, double lower, double upper)
        {
            if (double.IsNaN(value))
                return false;

            var logValue = Math.Log(value);

            return logValue <= lower || logValue >= upper;
        }
    }


    /// <summary>
    /// Removes outliers from salary data by applying IQR-based detection independently per position level.
    /// This prevents cross-level contamination where junior salaries affect senior outlier detection and vice versa.
    /// </summary>
    /// <param name="salaries">Collection of salary entities to filter.</param>
    /// <returns>Filtered collection with outliers removed per level.</returns>
    internal static IReadOnlyList<SalaryEntity> RemoveOutliersByLevel(IReadOnlyList<SalaryEntity> salaries) 
        => salaries.Where(s => s.Level != PositionLevel.Unknown)
            .GroupBy(s => s.Level)
            .SelectMany(g => g.Count() < MinimumSampleSizeForOutlierDetection ? [.. g] : RemoveOutliers([.. g]))
            .Concat(salaries.Where(s => s.Level == PositionLevel.Unknown))
            .ToList();


    /// <summary>
    /// Computes yearly salary statistics with separate datasets for global min/max and averages/medians.
    /// Uses globally-filtered data for min/max to remove extreme outliers while using per-level filtered data for central tendency.
    /// </summary>
    /// <param name="globalFilteredSalaries">Globally-filtered salary dataset for computing min/max (removes extreme outliers like $1 or $8M).</param>
    /// <param name="perLevelFilteredSalaries">Per-level filtered salary dataset for computing mean/median (prevents cross-level contamination).</param>
    /// <param name="includePerLevel">Whether to include per-level breakdowns.</param>
    /// <returns>Yearly statistics with global and per-level metrics.</returns>
    internal static YearlyCoreStats ComputeYearly(IReadOnlyList<SalaryEntity> globalFilteredSalaries, IReadOnlyList<SalaryEntity> perLevelFilteredSalaries, bool includePerLevel)
    {
        var minByYear = new Dictionary<string, double>();
        var maxByYear = new Dictionary<string, double>();
        var meanByYear = new Dictionary<string, double>();
        var medianByYear = new Dictionary<string, double>();

        foreach (var g in globalFilteredSalaries.GroupBy(s => s.Date.Year).OrderBy(g => g.Key))
            PopulateMinMax(g, g.Key.ToString(), minByYear, maxByYear);

        foreach (var g in perLevelFilteredSalaries.GroupBy(s => s.Date.Year).OrderBy(g => g.Key))
            PopulateMeanMedian(g, g.Key.ToString(), meanByYear, medianByYear);

        var perLevel = new Dictionary<string, YearlyCoreLevelStats>();
        if (includePerLevel)
        {
            foreach (var level in perLevelFilteredSalaries.Select(s => s.Level).Distinct().Where(l => l != PositionLevel.Unknown))
            {
                var stats = ComputeLevelStats(perLevelFilteredSalaries.Where(s => s.Level == level));
                if (stats is not null)
                    perLevel[level.ToString()] = stats;
            }
        }

        return new YearlyCoreStats(minByYear, maxByYear, meanByYear, medianByYear, perLevel);


        void PopulateMinMax(IEnumerable<SalaryEntity> group, string yearKey, Dictionary<string, double> min, Dictionary<string, double> max)
        {
            var lowers = group.Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance).Select(s => s.LowerBoundNormalized).ToArray();
            if (lowers.Length > 0)
                min[yearKey] = lowers.Min();

            var uppers = group.Where(s => Math.Abs(s.UpperBoundNormalized) > Tolerance).Select(s => s.UpperBoundNormalized).ToArray();
            if (uppers.Length > 0)
                max[yearKey] = uppers.Max();
        }


        void PopulateMeanMedian(IEnumerable<SalaryEntity> group, string yearKey, Dictionary<string, double> mean, Dictionary<string, double> median)
        {
            var pairs = group
                .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .Select(NormalizePair)
                .Where(v => !double.IsNaN(v))
                .ToArray();

            if (pairs.Length > 0)
            {
                mean[yearKey] = pairs.Mean();
                median[yearKey] = pairs.Median();
            }
        }


        YearlyCoreLevelStats? ComputeLevelStats(IEnumerable<SalaryEntity> levelSalaries)
        {
            var minByYear = new Dictionary<string, double>();
            var maxByYear = new Dictionary<string, double>();
            var meanByYear = new Dictionary<string, double>();
            var medianByYear = new Dictionary<string, double>();

            foreach (var g in levelSalaries.GroupBy(s => s.Date.Year).OrderBy(g => g.Key))
            {
                var yearKey = g.Key.ToString();
                PopulateMinMax(g, yearKey, minByYear, maxByYear);
                PopulateMeanMedian(g, yearKey, meanByYear, medianByYear);
            }

            return minByYear.Count > 0 || maxByYear.Count > 0 || meanByYear.Count > 0 || medianByYear.Count > 0
                ? new YearlyCoreLevelStats(minByYear, maxByYear, meanByYear, medianByYear)
                : null;
        }
    }


    internal static double NormalizePair(SalaryEntity salary)
    {
        if (double.IsNaN(salary.LowerBoundNormalized) && double.IsNaN(salary.UpperBoundNormalized))
            return double.NaN;

        if (double.IsNaN(salary.LowerBoundNormalized))
            return salary.UpperBoundNormalized;

        if (double.IsNaN(salary.UpperBoundNormalized))
            return salary.LowerBoundNormalized;

        return (salary.LowerBoundNormalized + salary.UpperBoundNormalized) / 2.0;
    }


    internal sealed record YearlyCoreLevelStats
    (
        Dictionary<string, double> MinimumByYear,
        Dictionary<string, double> MaximumByYear,
        Dictionary<string, double> AverageByYear,
        Dictionary<string, double> MedianByYear
    );


    internal sealed record YearlyCoreStats
    (
        Dictionary<string, double> MinimumByYear,
        Dictionary<string, double> MaximumByYear,
        Dictionary<string, double> AverageByYear,
        Dictionary<string, double> MedianByYear,
        Dictionary<string, YearlyCoreLevelStats> ByLevel
    );


    internal const int MinimumSampleSizeForOutlierDetection = 15;
    internal const double Tolerance = 1e-10;
}
