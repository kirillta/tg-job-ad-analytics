using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;

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
    public List<Models.Reports.Html.StackComparisonItem> BuildLastClosedMonth()
    {
        var lastClosedMonthEnd = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
        var lastClosedMonthStart = new DateOnly(lastClosedMonthEnd.Year, lastClosedMonthEnd.Month, 1);

        var query = from s in _dbContext.Salaries
                    join a in _dbContext.Ads on s.AdId equals a.Id
                    join ts in _dbContext.TechnologyStacks on a.StackId equals ts.Id
                    where s.Date >= lastClosedMonthStart && s.Date <= lastClosedMonthEnd && a.StackId != null
                    select new { ts.Id, ts.Name, s.LowerBoundNormalized, s.UpperBoundNormalized };

        var byStack = query.AsNoTracking().ToList()
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g =>
            {
                var values = g.Select(x => GetNormalizedValue(x.LowerBoundNormalized, x.UpperBoundNormalized))
                    .Where(v => !double.IsNaN(v))
                    .ToArray();

                var count = values.Length;
                var p25 = count > 0 ? values.Quantile(0.25) : double.NaN;
                var median = count > 0 ? values.Quantile(0.50) : double.NaN;
                var p75 = count > 0 ? values.Quantile(0.75) : double.NaN;

                return new Models.Reports.Html.StackComparisonItem
                {
                    StackId = g.Key.Id,
                    Name = g.Key.Name,
                    Label = g.Key.Name,
                    Count = count,
                    P25 = p25,
                    Median = median,
                    P75 = p75
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
                    select new { Year = s.Date.Year, ts.Id, ts.Name, s.LowerBoundNormalized, s.UpperBoundNormalized };

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
                            .Where(v => !double.IsNaN(v))
                            .ToArray();

                        var count = values.Length;
                        var p25 = count > 0 ? values.Quantile(0.25) : double.NaN;
                        var median = count > 0 ? values.Quantile(0.50) : double.NaN;
                        var p75 = count > 0 ? values.Quantile(0.75) : double.NaN;

                        return new Models.Reports.Html.StackComparisonItem
                        {
                            StackId = g.Key.Id,
                            Name = g.Key.Name,
                            Label = g.Key.Name,
                            Count = count,
                            P25 = p25,
                            Median = median,
                            P75 = p75
                        };
                    })
                    .OrderByDescending(x => x.Median)
                    .ToList()
            })
            .ToList();

        return byYear;
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
