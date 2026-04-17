using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports.Html;

namespace TgJobAdAnalytics.Services.Reports.Html;

/// <summary>
/// Builds datasets for client-side, viewer-driven stack comparison.
/// </summary>
public sealed class StackComparisonDataBuilder
{
    public StackComparisonDataBuilder(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    /// <summary>
    /// Builds per-stack salary stats for the last fully closed month.
    /// </summary>
    public List<StackComparisonItem> BuildLastClosedMonth()
    {
        var lastClosedMonthEnd = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
        var lastClosedMonthStart = new DateOnly(lastClosedMonthEnd.Year, lastClosedMonthEnd.Month, 1);

        var query = from s in _dbContext.Salaries
                    join a in _dbContext.Ads on s.AdId equals a.Id
                    join ts in _dbContext.TechnologyStacks on a.StackId equals ts.Id
                    where s.Date >= lastClosedMonthStart && s.Date <= lastClosedMonthEnd && a.StackId != null
                    select new { ts.Id, ts.Name, s.LowerBoundNormalized, s.UpperBoundNormalized, s.Level };

        var items = query.AsNoTracking().ToList();

        var byStack = items
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g =>
            {
                var values = g.Select(x => GetNormalizedValue(x.LowerBoundNormalized, x.UpperBoundNormalized))
                    .Where(v => !double.IsNaN(v) && v > 0)
                    .ToArray();

                var totalCount = g.Count();

                var perLevel = g
                    .GroupBy(x => x.Level)
                    .ToDictionary(
                        k => k.Key.ToString(),
                        v =>
                        {
                            var lvlValues = v
                                .Select(x => GetNormalizedValue(x.LowerBoundNormalized, x.UpperBoundNormalized))
                                .Where(val => !double.IsNaN(val) && val > 0)
                                .ToArray();

                            return new Models.Reports.Html.StackLevelStats
                            {
                                Count = v.Count(),
                                P10 = lvlValues.Length > 0 ? lvlValues.Quantile(0.10) : double.NaN,
                                P25 = lvlValues.Length > 0 ? lvlValues.Quantile(0.25) : double.NaN,
                                Median = lvlValues.Length > 0 ? lvlValues.Quantile(0.50) : double.NaN,
                                P75 = lvlValues.Length > 0 ? lvlValues.Quantile(0.75) : double.NaN,
                                P90 = lvlValues.Length > 0 ? lvlValues.Quantile(0.90) : double.NaN
                            };
                        });

                return new Models.Reports.Html.StackComparisonItem
                {
                    StackId = g.Key.Id,
                    Name = g.Key.Name,
                    Label = g.Key.Name,
                    Count = totalCount,
                    P10 = values.Length > 0 ? values.Quantile(0.10) : double.NaN,
                    P25 = values.Length > 0 ? values.Quantile(0.25) : double.NaN,
                    Median = values.Length > 0 ? values.Quantile(0.50) : double.NaN,
                    P75 = values.Length > 0 ? values.Quantile(0.75) : double.NaN,
                    P90 = values.Length > 0 ? values.Quantile(0.90) : double.NaN,
                    PerLevel = perLevel
                };
            })
            .OrderByDescending(x => x.Median)
            .ToList();

        return byStack;
    }


    /// <summary>
    /// Builds per-year per-stack salary stats across all available years.
    /// </summary>
    public List<Models.Reports.Html.StackComparisonYearGroup> BuildByYear()
    {
        var query = from s in _dbContext.Salaries
                    join a in _dbContext.Ads on s.AdId equals a.Id
                    join ts in _dbContext.TechnologyStacks on a.StackId equals ts.Id
                    where a.StackId != null
                    select new { Year = s.Date.Year, ts.Id, ts.Name, s.LowerBoundNormalized, s.UpperBoundNormalized, s.Level };

        var items = query.AsNoTracking().ToList();

        var byYear = items
            .GroupBy(x => x.Year)
            .OrderBy(g => g.Key)
            .Select(gYear => new Models.Reports.Html.StackComparisonYearGroup
            {
                Year = gYear.Key,
                Items = gYear
                    .GroupBy(x => new { x.Id, x.Name })
                    .Select(g =>
                    {
                        var values = g.Select(x => GetNormalizedValue(x.LowerBoundNormalized, x.UpperBoundNormalized))
                            .Where(v => !double.IsNaN(v) && v > 0)
                            .ToArray();

                        var totalCount = g.Count();

                        var perLevel = g
                            .GroupBy(x => x.Level)
                            .ToDictionary(
                                k => k.Key.ToString(),
                                v =>
                                {
                                    var lvlValues = v
                                        .Select(x => GetNormalizedValue(x.LowerBoundNormalized, x.UpperBoundNormalized))
                                        .Where(val => !double.IsNaN(val) && val > 0)
                                        .ToArray();

                                    return new Models.Reports.Html.StackLevelStats
                                    {
                                        Count = v.Count(),
                                        P10 = lvlValues.Length > 0 ? lvlValues.Quantile(0.10) : double.NaN,
                                        P25 = lvlValues.Length > 0 ? lvlValues.Quantile(0.25) : double.NaN,
                                        Median = lvlValues.Length > 0 ? lvlValues.Quantile(0.50) : double.NaN,
                                        P75 = lvlValues.Length > 0 ? lvlValues.Quantile(0.75) : double.NaN,
                                        P90 = lvlValues.Length > 0 ? lvlValues.Quantile(0.90) : double.NaN
                                    };
                                });

                        return new Models.Reports.Html.StackComparisonItem
                        {
                            StackId = g.Key.Id,
                            Name = g.Key.Name,
                            Label = g.Key.Name,
                            Count = totalCount,
                            P10 = values.Length > 0 ? values.Quantile(0.10) : double.NaN,
                            P25 = values.Length > 0 ? values.Quantile(0.25) : double.NaN,
                            Median = values.Length > 0 ? values.Quantile(0.50) : double.NaN,
                            P75 = values.Length > 0 ? values.Quantile(0.75) : double.NaN,
                            P90 = values.Length > 0 ? values.Quantile(0.90) : double.NaN,
                            PerLevel = perLevel
                        };
                    })
                    .OrderByDescending(x => x.Median)
                    .ToList()
            })
            .ToList();

        return byYear;
    }


    /// <summary>
    /// Builds per-stack salary stats for the last fully closed month using pre-loaded data.
    /// </summary>
    /// <param name="data">Pre-loaded salary and stack records from a master query.</param>
    public static List<StackComparisonItem> BuildLastClosedMonth(List<SalaryStackRecord> data)
    {
        var lastClosedMonthEnd = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
        var lastClosedMonthStart = new DateOnly(lastClosedMonthEnd.Year, lastClosedMonthEnd.Month, 1);

        return data
            .Where(r => r.StackId.HasValue && r.StackName != null && r.Salary.Date >= lastClosedMonthStart && r.Salary.Date <= lastClosedMonthEnd)
            .GroupBy(r => new { StackId = r.StackId!.Value, r.StackName })
            .Select(g =>
            {
                var values = g.Select(r => GetNormalizedValue(r.Salary.LowerBoundNormalized, r.Salary.UpperBoundNormalized))
                    .Where(v => !double.IsNaN(v) && v > 0)
                    .ToArray();

                var totalCount = g.Count();
                var stackName = g.Key.StackName!;

                var perLevel = g
                    .GroupBy(r => r.Salary.Level)
                    .ToDictionary(
                        k => k.Key.ToString(),
                        v =>
                        {
                            var lvlValues = v
                                .Select(r => GetNormalizedValue(r.Salary.LowerBoundNormalized, r.Salary.UpperBoundNormalized))
                                .Where(val => !double.IsNaN(val) && val > 0)
                                .ToArray();

                            return new StackLevelStats
                            {
                                Count = v.Count(),
                                P10 = lvlValues.Length > 0 ? lvlValues.Quantile(0.10) : double.NaN,
                                P25 = lvlValues.Length > 0 ? lvlValues.Quantile(0.25) : double.NaN,
                                Median = lvlValues.Length > 0 ? lvlValues.Quantile(0.50) : double.NaN,
                                P75 = lvlValues.Length > 0 ? lvlValues.Quantile(0.75) : double.NaN,
                                P90 = lvlValues.Length > 0 ? lvlValues.Quantile(0.90) : double.NaN
                            };
                        });

                return new StackComparisonItem
                {
                    StackId = g.Key.StackId,
                    Name = stackName,
                    Label = stackName,
                    Count = totalCount,
                    P10 = values.Length > 0 ? values.Quantile(0.10) : double.NaN,
                    P25 = values.Length > 0 ? values.Quantile(0.25) : double.NaN,
                    Median = values.Length > 0 ? values.Quantile(0.50) : double.NaN,
                    P75 = values.Length > 0 ? values.Quantile(0.75) : double.NaN,
                    P90 = values.Length > 0 ? values.Quantile(0.90) : double.NaN,
                    PerLevel = perLevel
                };
            })
            .OrderByDescending(x => x.Median)
            .ToList();
    }


    /// <summary>
    /// Builds per-year per-stack salary stats across all available years using pre-loaded data.
    /// </summary>
    /// <param name="data">Pre-loaded salary and stack records from a master query.</param>
    public static List<StackComparisonYearGroup> BuildByYear(List<SalaryStackRecord> data)
    {
        return data
            .Where(r => r.StackId.HasValue && r.StackName != null)
            .GroupBy(r => r.Salary.Date.Year)
            .OrderBy(g => g.Key)
            .Select(gYear => new StackComparisonYearGroup
            {
                Year = gYear.Key,
                Items = gYear
                    .GroupBy(r => new { StackId = r.StackId!.Value, r.StackName })
                    .Select(g =>
                    {
                        var values = g.Select(r => GetNormalizedValue(r.Salary.LowerBoundNormalized, r.Salary.UpperBoundNormalized))
                            .Where(v => !double.IsNaN(v) && v > 0)
                            .ToArray();

                        var totalCount = g.Count();
                        var stackName = g.Key.StackName!;

                        var perLevel = g
                            .GroupBy(r => r.Salary.Level)
                            .ToDictionary(
                                k => k.Key.ToString(),
                                v =>
                                {
                                    var lvlValues = v
                                        .Select(r => GetNormalizedValue(r.Salary.LowerBoundNormalized, r.Salary.UpperBoundNormalized))
                                        .Where(val => !double.IsNaN(val) && val > 0)
                                        .ToArray();

                                    return new StackLevelStats
                                    {
                                        Count = v.Count(),
                                        P10 = lvlValues.Length > 0 ? lvlValues.Quantile(0.10) : double.NaN,
                                        P25 = lvlValues.Length > 0 ? lvlValues.Quantile(0.25) : double.NaN,
                                        Median = lvlValues.Length > 0 ? lvlValues.Quantile(0.50) : double.NaN,
                                        P75 = lvlValues.Length > 0 ? lvlValues.Quantile(0.75) : double.NaN,
                                        P90 = lvlValues.Length > 0 ? lvlValues.Quantile(0.90) : double.NaN
                                    };
                                });

                        return new StackComparisonItem
                        {
                            StackId = g.Key.StackId,
                            Name = stackName,
                            Label = stackName,
                            Count = totalCount,
                            P10 = values.Length > 0 ? values.Quantile(0.10) : double.NaN,
                            P25 = values.Length > 0 ? values.Quantile(0.25) : double.NaN,
                            Median = values.Length > 0 ? values.Quantile(0.50) : double.NaN,
                            P75 = values.Length > 0 ? values.Quantile(0.75) : double.NaN,
                            P90 = values.Length > 0 ? values.Quantile(0.90) : double.NaN,
                            PerLevel = perLevel
                        };
                    })
                    .OrderByDescending(x => x.Median)
                    .ToList()
            })
            .ToList();
    }


    private static double GetNormalizedValue(double lower, double upper)
    {
        var hasLowerBound = !double.IsNaN(lower);
        var hasUpperBound = !double.IsNaN(upper);

        if (!hasLowerBound && !hasUpperBound)
            return double.NaN;

        if (!hasLowerBound)
            return upper;

        if (!hasUpperBound)
            return lower;

        return (lower + upper) / 2.0;
    }


    private readonly ApplicationDbContext _dbContext;
}
