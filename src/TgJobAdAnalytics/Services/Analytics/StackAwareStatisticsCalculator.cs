using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Html;

namespace TgJobAdAnalytics.Services.Analytics;

/// <summary>
/// Calculates multi-series salary statistics for client-side stack filtering.
/// </summary>
public sealed class StackAwareStatisticsCalculator
{
    public StackAwareStatisticsCalculator(ApplicationDbContext dbContext, IOptions<StackFilteringConfiguration> stackFilteringOptions)
    {
        _dbContext = dbContext;
        _stackFilteringConfig = stackFilteringOptions.Value;
    }


    /// <summary>
    /// Calculates salary statistics with global and per-stack breakdowns.
    /// </summary>
    public MultiSeriesSalaryStatistics CalculateStatistics(DateTime startDate, DateTime endDate)
    {
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        var salariesQuery = from s in _dbContext.Salaries
            join a in _dbContext.Ads on s.AdId equals a.Id
            where s.Date >= startDateOnly && s.Date <= endDateOnly
            select new SalaryWithStack { Salary = s, StackId = a.StackId };

        var salariesWithStack = salariesQuery.AsNoTracking().ToList();

        var filteredSalaries = RemoveOutliers([.. salariesWithStack.Select(x => x.Salary)]);
        var filteredSalariesWithStack = salariesWithStack
            .Where(s => filteredSalaries.Any(f => f.Id == s.Salary.Id))
            .ToList();

        var global = CalculateGlobalStatistics(filteredSalaries);
        var byStack = CalculatePerStackStatistics(filteredSalariesWithStack);
        var stacksSummary = CalculateStacksSummary(salariesWithStack);
        var yearlyStats = CalculateYearlyStatistics(filteredSalaries);
        var yearlyByStack = CalculateYearlyByStack(filteredSalariesWithStack);

        return new MultiSeriesSalaryStatistics
        {
            Global = global,
            ByStack = byStack,
            YearlyStats = yearlyStats,
            YearlyByStack = yearlyByStack,
            Metadata = new StatisticsMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                DateRangeFrom = startDate,
                DateRangeTo = endDate,
                TotalJobs = salariesWithStack.Count,
                StackCount = byStack.Count
            },
            Stacks = stacksSummary
        };
    }


    private GlobalSalaryStatistics CalculateGlobalStatistics(List<Data.Salaries.SalaryEntity> salaries)
    {
        var trends = CalculateTrends(salaries);
        var distribution = CalculateDistribution(salaries);
        var aggregates = CalculateAggregates(salaries);

        return new GlobalSalaryStatistics
        {
            Trends = trends,
            Distribution = distribution,
            Aggregates = aggregates
        };
    }


    private Dictionary<string, StackSalaryStatistics> CalculatePerStackStatistics(List<SalaryWithStack> salariesWithStack)
    {
        var stacksQuery = from item in salariesWithStack
            where item.StackId != null
            group item by item.StackId into g
            select new
            {
                StackId = g.Key!.Value,
                Count = g.Count()
            };

        var topStacks = stacksQuery
            .OrderByDescending(x => x.Count)
            .Where(x => x.Count >= _stackFilteringConfig.MinimumJobCount)
            .Take(_stackFilteringConfig.TopStacksLimit)
            .Select(x => x.StackId)
            .ToHashSet();

        var stackNameMap = _dbContext.TechnologyStacks
            .Where(ts => topStacks.Contains(ts.Id))
            .ToDictionary(ts => ts.Id, ts => ts.Name);

        var result = new Dictionary<string, StackSalaryStatistics>();

        foreach (var stackId in topStacks)
        {
            var stackSalaries = salariesWithStack
                .Where(x => x.StackId == stackId)
                .Select(x => x.Salary)
                .ToList();

            if (stackSalaries.Count == 0)
                continue;

            if (!stackNameMap.TryGetValue(stackId, out var stackName))
                stackName = "Unknown";

            var trends = CalculateTrends(stackSalaries);
            var distribution = CalculateDistribution(stackSalaries);
            var aggregates = CalculateAggregates(stackSalaries);

            result[stackName] = new StackSalaryStatistics
            {
                Trends = trends,
                Distribution = distribution,
                Aggregates = aggregates
            };
        }

        return result;
    }


    private Dictionary<string, YearlySalaryStatistics> CalculateYearlyByStack(List<SalaryWithStack> salariesWithStack)
    {
        var stacksQuery = from item in salariesWithStack
            where item.StackId != null
            group item by item.StackId into g
            select new
            {
                StackId = g.Key!.Value,
                Count = g.Count()
            };

        var topStacks = stacksQuery
            .OrderByDescending(x => x.Count)
            .Where(x => x.Count >= _stackFilteringConfig.MinimumJobCount)
            .Take(_stackFilteringConfig.TopStacksLimit)
            .Select(x => x.StackId)
            .ToList();

        var stackNameMap = _dbContext.TechnologyStacks
            .Where(ts => topStacks.Contains(ts.Id))
            .ToDictionary(ts => ts.Id, ts => ts.Name);

        var result = new Dictionary<string, YearlySalaryStatistics>(StringComparer.OrdinalIgnoreCase);

        foreach (var stackId in topStacks)
        {
            var stackSalaries = salariesWithStack
                .Where(x => x.StackId == stackId)
                .Select(x => x.Salary)
                .ToList();

            if (stackSalaries.Count == 0)
                continue;

            var y = CalculateYearlyStatistics(stackSalaries);
            var name = stackNameMap.TryGetValue(stackId, out var n) ? n : "Unknown";
            result[name] = y;
        }

        return result;
    }


    private List<StackSummary> CalculateStacksSummary(List<SalaryWithStack> salariesWithStack)
    {
        var totalCount = salariesWithStack.Count;
        var stackGroups = from item in salariesWithStack
            where item.StackId != null
            group item by item.StackId into g
            select new
            {
                StackId = g.Key!.Value,
                Count = g.Count()
            };

        var topStacks = stackGroups
            .OrderByDescending(x => x.Count)
            .Where(x => x.Count >= _stackFilteringConfig.MinimumJobCount)
            .Take(_stackFilteringConfig.TopStacksLimit)
            .ToList();

        var stackIds = topStacks.Select(x => x.StackId).ToList();
        var stackNameMap = _dbContext.TechnologyStacks
            .Where(ts => stackIds.Contains(ts.Id))
            .ToDictionary(ts => ts.Id, ts => ts.Name);

        return [.. topStacks.Select(s =>
        {
            if (!stackNameMap.TryGetValue(s.StackId, out var stackName))
                stackName = "Unknown";

            return new StackSummary
            {
                Id = stackName,
                Name = stackName,
                JobCount = s.Count,
                Percentage = totalCount > 0 ? (double)s.Count / totalCount * 100 : 0
            };
        })];
    }


    private static List<TrendDataPoint> CalculateTrends(List<Data.Salaries.SalaryEntity> salaries)
    {
        var trendData = salaries
            .GroupBy(s => new { s.Date.Year, s.Date.Month })
            .Select(g =>
            {
                var values = g.Select(GetNormalizedValue)
                    .Where(v => !double.IsNaN(v))
                    .ToArray();

                if (values.Length == 0)
                    return null;

                return new TrendDataPoint
                {
                    Date = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Median = values.Median(),
                    Mean = values.Mean(),
                    Count = values.Length,
                    P25 = values.Quantile(0.25),
                    P75 = values.Quantile(0.75)
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x!.Date)
            .Select(x => x!)
            .ToList();

        return trendData;
    }


    private static List<DistributionBucket> CalculateDistribution(List<Data.Salaries.SalaryEntity> salaries)
    {
        var values = salaries.Select(GetNormalizedValue)
            .Where(v => !double.IsNaN(v))
            .ToList();

        if (values.Count == 0)
            return [];

        var buckets = new List<(string Label, double Min, double Max)>
        {
            ("0-50k", 0, 50000),
            ("50k-100k", 50000, 100000),
            ("100k-150k", 100000, 150000),
            ("150k-200k", 150000, 200000),
            ("200k-250k", 200000, 250000),
            ("250k-300k", 250000, 300000),
            ("300k+", 300000, double.MaxValue)
        };

        var totalCount = values.Count;

        return [.. buckets.Select(bucket =>
        {
            var count = values.Count(v => v >= bucket.Min && v < bucket.Max);
            return new DistributionBucket
            {
                Bucket = bucket.Label,
                Count = count,
                Percentage = totalCount > 0 ? (double)count / totalCount * 100 : 0
            };
        })];
    }


    private static AggregateStatistics CalculateAggregates(List<Data.Salaries.SalaryEntity> salaries)
    {
        var values = salaries.Select(GetNormalizedValue)
            .Where(v => !double.IsNaN(v))
            .ToArray();

        if (values.Length == 0)
        {
            return new AggregateStatistics
            {
                TotalJobs = 0,
                MedianSalary = 0,
                MeanSalary = 0,
                Percentiles = new Dictionary<string, double>()
            };
        }

        return new AggregateStatistics
        {
            TotalJobs = values.Length,
            MedianSalary = values.Median(),
            MeanSalary = values.Mean(),
            Percentiles = new Dictionary<string, double>
            {
                ["p10"] = values.Quantile(0.10),
                ["p25"] = values.Quantile(0.25),
                ["p50"] = values.Median(),
                ["p75"] = values.Quantile(0.75),
                ["p90"] = values.Quantile(0.90)
            }
        };
    }


    private static double GetNormalizedValue(Data.Salaries.SalaryEntity salary)
    {
        var hasLowerBound = !double.IsNaN(salary.LowerBoundNormalized);
        var hasUpperBound = !double.IsNaN(salary.UpperBoundNormalized);

        if (!hasLowerBound && !hasUpperBound)
            return double.NaN;

        if (!hasLowerBound)
            return salary.UpperBoundNormalized;

        if (!hasUpperBound)
            return salary.LowerBoundNormalized;

        return (salary.LowerBoundNormalized + salary.UpperBoundNormalized) / 2.0;
    }


    private static YearlySalaryStatistics CalculateYearlyStatistics(List<Data.Salaries.SalaryEntity> salaries)
    {
        var minimumByYear = salaries
            .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance)
            .GroupBy(s => s.Date.Year)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Select(s => s.LowerBoundNormalized).Min()
            );

        var maximumByYear = salaries
            .Where(s => Math.Abs(s.UpperBoundNormalized) > Tolerance)
            .GroupBy(s => s.Date.Year)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Select(s => s.UpperBoundNormalized).Max()
            );

        var averageByYear = salaries
            .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
            .GroupBy(s => s.Date.Year)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Select(GetNormalizedValue).Where(v => !double.IsNaN(v)).Average()
            );

        var medianByYear = salaries
            .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
            .GroupBy(s => s.Date.Year)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Select(GetNormalizedValue).Where(v => !double.IsNaN(v)).Median()
            );

        var byLevel = new Dictionary<string, LevelYearlyStatistics>();
        var levels = salaries.Select(s => s.Level).Distinct().Where(l => l != Models.Levels.Enums.PositionLevel.Unknown);

        foreach (var level in levels)
        {
            var levelSalaries = salaries.Where(s => s.Level == level).ToList();

            var levelMin = levelSalaries
                .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance)
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(s => s.LowerBoundNormalized).Min()
                );

            var levelMax = levelSalaries
                .Where(s => Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(s => s.UpperBoundNormalized).Max()
                );

            var levelAvg = levelSalaries
                .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(GetNormalizedValue).Where(v => !double.IsNaN(v)).Average()
                );

            var levelMedian = levelSalaries
                .Where(s => Math.Abs(s.LowerBoundNormalized) > Tolerance && Math.Abs(s.UpperBoundNormalized) > Tolerance)
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key)
                .Where(g => g.Select(GetNormalizedValue).Where(v => !double.IsNaN(v)).Any())
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(GetNormalizedValue).Where(v => !double.IsNaN(v)).Median()
                );

            if (levelMin.Count > 0 || levelMax.Count > 0 || levelAvg.Count > 0 || levelMedian.Count > 0)
            {
                byLevel[level.ToString()] = new LevelYearlyStatistics
                {
                    MinimumByYear = levelMin,
                    MaximumByYear = levelMax,
                    AverageByYear = levelAvg,
                    MedianByYear = levelMedian
                };
            }
        }

        return new YearlySalaryStatistics
        {
            MinimumByYear = minimumByYear,
            MaximumByYear = maximumByYear,
            AverageByYear = averageByYear,
            MedianByYear = medianByYear,
            ByLevel = byLevel
        };
    }


    private static List<Data.Salaries.SalaryEntity> RemoveOutliers(List<Data.Salaries.SalaryEntity> salaries)
    {
        var excludedIds = GetExcludedIds(salaries, s => s.LowerBoundNormalized)
            .Concat(GetExcludedIds(salaries, s => s.UpperBoundNormalized))
            .ToHashSet();

        return salaries.Where(s => !excludedIds.Contains(s.Id)).ToList();


        static IEnumerable<Guid> GetExcludedIds(List<Data.Salaries.SalaryEntity> salaries, Func<Data.Salaries.SalaryEntity, double> selector)
        {
            var validLogValues = salaries
                .Select(selector)
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

            return salaries
                .Where(s => IsOutlier(selector(s), lowerThreshold, upperThreshold))
                .Select(s => s.Id);
        }


        static bool IsOutlier(double value, double lowerThreshold, double upperThreshold)
        {
            if (double.IsNaN(value))
                return false;

            var logValue = Math.Log(value);
            return logValue <= lowerThreshold || logValue >= upperThreshold;
        }
    }


    private const double Tolerance = 1e-10;


    private readonly ApplicationDbContext _dbContext;
    private readonly StackFilteringConfiguration _stackFilteringConfig;


    private sealed class SalaryWithStack
    {
        public Data.Salaries.SalaryEntity Salary { get; set; } = null!;
        public Guid? StackId { get; set; }
    }
}
