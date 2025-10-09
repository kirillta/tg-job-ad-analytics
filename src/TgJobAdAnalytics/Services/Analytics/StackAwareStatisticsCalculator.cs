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

        var salariesQuery = from s in _dbContext.Salaries
            join a in _dbContext.Ads on s.AdId equals a.Id
            where s.Date >= startDateOnly && s.Date <= endDateOnly
            select new SalaryWithStack { Salary = s, StackId = a.StackId };

        var salariesWithStack = salariesQuery.AsNoTracking().ToList();

        var filteredSalaries = SalaryStatisticsCore.RemoveOutliers([.. salariesWithStack.Select(x => x.Salary)]).ToList();
        var filteredIds = filteredSalaries.Select(s => s.Id).ToHashSet();
        var filteredSalariesWithStack = salariesWithStack
            .Where(s => filteredIds.Contains(s.Salary.Id))
            .ToList();

        var global = CalculateGlobalStatistics(filteredSalaries);
        var byStack = CalculatePerStackStatistics(filteredSalariesWithStack);
        var stacksSummary = CalculateStacksSummary(salariesWithStack);
        var yearlyStats = BuildYearly(filteredSalaries, includePerLevel: true);
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


    private static GlobalSalaryStatistics CalculateGlobalStatistics(List<Data.Salaries.SalaryEntity> salaries)
    {
        var distribution = CalculateDistribution(salaries);
        var aggregates = CalculateAggregates(salaries);

        return new GlobalSalaryStatistics
        {
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

            var distribution = CalculateDistribution(stackSalaries);
            var aggregates = CalculateAggregates(stackSalaries);

            result[stackName] = new StackSalaryStatistics
            {
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

            var stats = SalaryStatisticsCore.ComputeYearly(stackSalaries, includePerLevel: true);
            var yearly = new YearlySalaryStatistics
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

            var name = stackNameMap.TryGetValue(stackId, out var n) ? n : "Unknown";
            result[name] = yearly;
        }

        return result;
    }


    private static YearlySalaryStatistics BuildYearly(List<Data.Salaries.SalaryEntity> salaries, bool includePerLevel)
    {
        var stats = SalaryStatisticsCore.ComputeYearly(salaries, includePerLevel: includePerLevel);
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


    private sealed class SalaryWithStack
    {
        public Data.Salaries.SalaryEntity Salary { get; set; } = null!;
        public Guid? StackId { get; set; }
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly StackFilteringConfiguration _stackFilteringConfig;
}
