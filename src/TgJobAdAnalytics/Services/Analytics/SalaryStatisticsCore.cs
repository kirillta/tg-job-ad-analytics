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

        return salaries.Where(s => !excludedIds.Contains(s.Id)).ToList();


        static IEnumerable<Guid> GetExcludedIds(IReadOnlyList<SalaryEntity> source, Func<SalaryEntity, double> selector)
        {
            var validLogValues = source
                .Select(s => selector(s))
                .Where(v => !double.IsNaN(v) && Math.Abs(v) > Tolerance)
                .Select(v => Math.Log(v))
                .ToArray();

            if (validLogValues.Length == 0)
                return Enumerable.Empty<Guid>();

            var q1 = validLogValues.Quantile(0.25);
            var q3 = validLogValues.Quantile(0.75);
            var iqr = q3 - q1;
            var lowerThreshold = q1 - 1.5 * iqr;
            var upperThreshold = q3 + 1.5 * iqr;

            return source
                .Where(s => IsOutlier(selector(s), lowerThreshold, upperThreshold))
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


    internal static YearlyCoreStats ComputeYearly(IReadOnlyList<SalaryEntity> salaries, bool includePerLevel)
    {
        var minByYear = new Dictionary<string, double>();
        var maxByYear = new Dictionary<string, double>();
        var meanByYear = new Dictionary<string, double>();
        var medianByYear = new Dictionary<string, double>();

        // Pre-group by year once
        var byYear = salaries.GroupBy(s => s.Date.Year).OrderBy(g => g.Key).ToList();
        foreach (var g in byYear)
        {
            var yearKey = g.Key.ToString();

            var lowers = g.Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance).Select(s => s.LowerBoundNormalized).ToArray();
            if (lowers.Length > 0)
                minByYear[yearKey] = lowers.Min();

            var uppers = g.Where(s => Math.Abs(s.UpperBoundNormalized) > Tolerance).Select(s => s.UpperBoundNormalized).ToArray();
            if (uppers.Length > 0)
                maxByYear[yearKey] = uppers.Max();

            var normalizedPairs = g
                .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .Select(NormalizePair)
                .Where(v => !double.IsNaN(v))
                .ToArray();

            if (normalizedPairs.Length > 0)
            {
                meanByYear[yearKey] = normalizedPairs.Mean();
                medianByYear[yearKey] = normalizedPairs.Median();
            }
        }

        var perLevel = new Dictionary<string, YearlyCoreLevelStats>();
        if (includePerLevel)
        {
            var levels = salaries.Select(s => s.Level).Distinct().Where(l => l != PositionLevel.Unknown).ToArray();
            foreach (var level in levels)
            {
                var levelSalaries = salaries.Where(s => s.Level == level).ToList();
                var lm = new Dictionary<string, double>();
                var lx = new Dictionary<string, double>();
                var la = new Dictionary<string, double>();
                var lmed = new Dictionary<string, double>();

                var levelByYear = levelSalaries.GroupBy(s => s.Date.Year).OrderBy(g => g.Key).ToList();
                foreach (var g in levelByYear)
                {
                    var yearKey = g.Key.ToString();

                    var lowers = g.Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance).Select(s => s.LowerBoundNormalized).ToArray();
                    if (lowers.Length > 0)
                        lm[yearKey] = lowers.Min();

                    var uppers = g.Where(s => Math.Abs(s.UpperBoundNormalized) > Tolerance).Select(s => s.UpperBoundNormalized).ToArray();
                    if (uppers.Length > 0)
                        lx[yearKey] = uppers.Max();

                    var normalizedPairs = g
                        .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                        .Select(NormalizePair)
                        .Where(v => !double.IsNaN(v))
                        .ToArray();

                    if (normalizedPairs.Length > 0)
                    {
                        la[yearKey] = normalizedPairs.Mean();
                        lmed[yearKey] = normalizedPairs.Median();
                    }
                }

                if (lm.Count > 0 || lx.Count > 0 || la.Count > 0 || lmed.Count > 0)
                    perLevel[level.ToString()] = new YearlyCoreLevelStats(lm, lx, la, lmed);
            }
        }

        return new YearlyCoreStats(minByYear, maxByYear, meanByYear, medianByYear, perLevel);
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


    internal sealed record YearlyCoreLevelStats(
        Dictionary<string, double> MinimumByYear,
        Dictionary<string, double> MaximumByYear,
        Dictionary<string, double> AverageByYear,
        Dictionary<string, double> MedianByYear);

    internal sealed record YearlyCoreStats(
        Dictionary<string, double> MinimumByYear,
        Dictionary<string, double> MaximumByYear,
        Dictionary<string, double> AverageByYear,
        Dictionary<string, double> MedianByYear,
        Dictionary<string, YearlyCoreLevelStats> ByLevel);


    internal const double Tolerance = 1e-10;
}
