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
    public MultiSeriesSalaryStatistics CalculateStatistics(in DateTime startDate, in DateTime endDate)
    {
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        var salariesWithStack = _dbContext.Salaries
            .Join(_dbContext.Ads, s => s.AdId, a => a.Id, (s, a) => new SalaryWithStack(s, a.StackId))
            .Where(x => x.Salary.Date >= startDateOnly && x.Salary.Date <= endDateOnly)
            .AsNoTracking()
            .ToList();

        return BuildStatistics(salariesWithStack, startDate, endDate);
    }


    /// <summary>
    /// Calculates salary statistics with global and per-stack breakdowns using pre-loaded data,
    /// avoiding a redundant database round-trip when the caller already holds the full salary dataset.
    /// </summary>
    /// <param name="preloaded">Pre-loaded salary and stack records from a master query.</param>
    /// <param name="startDate">Start of the date range to include.</param>
    /// <param name="endDate">End of the date range to include.</param>
    public MultiSeriesSalaryStatistics CalculateStatistics(List<SalaryStackRecord> preloaded, in DateTime startDate, in DateTime endDate)
    {
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        var salariesWithStack = preloaded
            .Where(r => r.Salary.Date >= startDateOnly && r.Salary.Date <= endDateOnly)
            .Select(r => new SalaryWithStack(r.Salary, r.StackId))
            .ToList();

        return BuildStatistics(salariesWithStack, startDate, endDate);
    }


    private MultiSeriesSalaryStatistics BuildStatistics(List<SalaryWithStack> salariesWithStack, in DateTime startDate, in DateTime endDate)
    {
        var allSalaries = salariesWithStack.Select(x => x.Salary).ToList();

        var globalFilteredSalaries = SalaryStatisticsCore.RemoveOutliers([.. allSalaries]).ToList();
        var perLevelFilteredSalaries = SalaryStatisticsCore.RemoveOutliersByLevel([.. allSalaries]).ToList();
        var perLevelFilteredIds = perLevelFilteredSalaries.Select(s => s.Id).ToHashSet();
        var filteredSalariesWithStack = salariesWithStack
            .Where(s => perLevelFilteredIds.Contains(s.Salary.Id))
            .ToList();

        var byStack = CalculatePerStackStatistics(filteredSalariesWithStack);

        return new MultiSeriesSalaryStatistics
        {
            Global = CalculateGlobalStatistics(perLevelFilteredSalaries),
            ByStack = byStack,
            YearlyStats = ToYearlySalaryStatistics(SalaryStatisticsCore.ComputeYearly(globalFilteredSalaries, perLevelFilteredSalaries, includePerLevel: true)),
            YearlyByStack = CalculateYearlyByStack(filteredSalariesWithStack),
            Metadata = new StatisticsMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                DateRangeFrom = startDate,
                DateRangeTo = endDate,
                TotalJobs = salariesWithStack.Count,
                StackCount = byStack.Count
            },
            Stacks = CalculateStacksSummary(salariesWithStack)
        };
    }


    private static GlobalSalaryStatistics CalculateGlobalStatistics(List<Data.Salaries.SalaryEntity> salaries) 
        => new()
        {
            Distribution = CalculateDistribution(salaries),
            Aggregates = CalculateAggregates(salaries)
        };


    private Dictionary<string, StackSalaryStatistics> CalculatePerStackStatistics(List<SalaryWithStack> salariesWithStack)
    {
        var (topStackIds, stackNameMap) = ResolveTopStacks(salariesWithStack);
        var result = new Dictionary<string, StackSalaryStatistics>();

        foreach (var stackId in topStackIds)
        {
            var stackSalaries = salariesWithStack
                .Where(x => x.StackId == stackId)
                .Select(x => x.Salary)
                .ToList();

            if (stackSalaries.Count == 0)
                continue;

            var stackName = stackNameMap.GetValueOrDefault(stackId, "Unknown");

            result[stackName] = new StackSalaryStatistics
            {
                Distribution = CalculateDistribution(stackSalaries),
                Aggregates = CalculateAggregates(stackSalaries)
            };
        }

        return result;
    }


    private Dictionary<string, YearlySalaryStatistics> CalculateYearlyByStack(List<SalaryWithStack> salariesWithStack)
    {
        var (topStackIds, stackNameMap) = ResolveTopStacks(salariesWithStack);
        var result = new Dictionary<string, YearlySalaryStatistics>(StringComparer.OrdinalIgnoreCase);

        foreach (var stackId in topStackIds)
        {
            var stackSalaries = salariesWithStack
                .Where(x => x.StackId == stackId)
                .Select(x => x.Salary)
                .ToList();

            if (stackSalaries.Count == 0)
                continue;

            var globalFiltered = SalaryStatisticsCore.RemoveOutliers(stackSalaries).ToList();
            var perLevelFiltered = SalaryStatisticsCore.RemoveOutliersByLevel(stackSalaries).ToList();
            var coreStats = SalaryStatisticsCore.ComputeYearly(globalFiltered, perLevelFiltered, includePerLevel: true);

            var name = stackNameMap.GetValueOrDefault(stackId, "Unknown");
            result[name] = ToYearlySalaryStatistics(coreStats);
        }

        return result;
    }


    private List<StackSummary> CalculateStacksSummary(List<SalaryWithStack> salariesWithStack)
    {
        var totalCount = salariesWithStack.Count;
        var (_, stackNameMap, stackCounts) = ResolveTopStacksWithCounts(salariesWithStack);

        return [.. stackCounts.Select(pair =>
        {
            var stackName = stackNameMap.GetValueOrDefault(pair.Key, "Unknown");
            return new StackSummary
            {
                Id = stackName,
                Name = stackName,
                JobCount = pair.Value,
                Percentage = totalCount > 0 ? (double)pair.Value / totalCount * 100 : 0
            };
        })];
    }


    private (HashSet<Guid> TopStackIds, Dictionary<Guid, string> StackNameMap) ResolveTopStacks(List<SalaryWithStack> salariesWithStack)
    {
        var (topStackIds, stackNameMap, _) = ResolveTopStacksWithCounts(salariesWithStack);
        return (topStackIds, stackNameMap);
    }


    private (HashSet<Guid> TopStackIds, Dictionary<Guid, string> StackNameMap, List<KeyValuePair<Guid, int>> StackCounts) ResolveTopStacksWithCounts(List<SalaryWithStack> salariesWithStack)
    {
        var stackCounts = salariesWithStack
            .Where(x => x.StackId != null)
            .GroupBy(x => x.StackId!.Value)
            .Select(g => new KeyValuePair<Guid, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Where(x => x.Value >= _stackFilteringConfig.MinimumJobCount)
            .Take(_stackFilteringConfig.TopStacksLimit)
            .ToList();

        var topStackIds = stackCounts.Select(x => x.Key).ToHashSet();

        var stackNameMap = _dbContext.TechnologyStacks
            .Where(ts => topStackIds.Contains(ts.Id))
            .ToDictionary(ts => ts.Id, ts => ts.Name);

        return (topStackIds, stackNameMap, stackCounts);
    }


    private static YearlySalaryStatistics ToYearlySalaryStatistics(SalaryStatisticsCore.YearlyCoreStats stats)
    {
        return new YearlySalaryStatistics
        {
            MinimumByYear = stats.MinimumByYear,
            MaximumByYear = stats.MaximumByYear,
            AverageByYear = stats.AverageByYear,
            MedianByYear = stats.MedianByYear,
            ByLevel = stats.ByLevel.ToDictionary(kv => kv.Key, kv => new LevelYearlyStatistics
            {
                MinimumByYear = kv.Value.MinimumByYear,
                MaximumByYear = kv.Value.MaximumByYear,
                AverageByYear = kv.Value.AverageByYear,
                MedianByYear = kv.Value.MedianByYear
            })
        };
    }


    private static List<DistributionBucket> CalculateDistribution(List<Data.Salaries.SalaryEntity> salaries)
    {
        var values = salaries.Select(SalaryStatisticsCore.NormalizePair)
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
        var values = salaries.Select(SalaryStatisticsCore.NormalizePair)
            .Where(v => !double.IsNaN(v))
            .ToArray();

        if (values.Length == 0)
        {
            return new AggregateStatistics
            {
                TotalJobs = 0,
                MedianSalary = 0,
                MeanSalary = 0,
                Percentiles = []
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


    private sealed record SalaryWithStack(Data.Salaries.SalaryEntity Salary, Guid? StackId);

    private readonly ApplicationDbContext _dbContext;
    private readonly StackFilteringConfiguration _stackFilteringConfig;
}
