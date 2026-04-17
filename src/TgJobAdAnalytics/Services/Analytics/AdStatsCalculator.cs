using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Enums;

namespace TgJobAdAnalytics.Services.Analytics;

/// <summary>
/// Provides analytics helpers for computing advertisement statistics and packaging them into report groups.
/// </summary>
public sealed class AdStatsCalculator
{
    /// <summary>
    /// Generates the advertisement statistics report group composed of several individual reports
    /// (top months, monthly distribution, yearly counts, etc.).
    /// </summary>
    /// <param name="salaries">Collection of salary entities extracted from advertisements.</param>
    /// <param name="adStackMapping">Mapping of ad identifiers to their technology stack names.</param>
    /// <returns>A <see cref="ReportGroup"/> containing the computed advertisement statistics reports.</returns>
    public static ReportGroup GenerateAll(List<SalaryEntity> salaries, Dictionary<Guid, string> adStackMapping)
    {
        var reports = new List<Report>
        {
            GetNumberOfAdsByYearAndMonth(salaries, adStackMapping),
            GetTopMonthsByAdCount(salaries),
            GetMonthlyAdCounts(salaries),
            GetYearlyAdCounts(salaries),
        };

        return new ReportGroup("group.ads.stats", reports);
    }


    private static Report GetTopMonthsByAdCount(List<SalaryEntity> salaries)
    {
        var results = salaries
            .GroupBy(salary => new { salary.Date.Year, salary.Date.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .Take(3)
            .ToDictionary(group => group.Year + " " + group.Month.ToString("00"), group => (double) group.Count); // key: "YYYY MM"

        return new Report("report.ads.top_months", results, ChartType.None);
    }


    private static Report GetNumberOfAdsByYearAndMonth(List<SalaryEntity> salaries, Dictionary<Guid, string> adStackMapping)
    {
        var baseResults = GetMonthlyCountsFromSalaries(salaries);

        var seriesOverlays = adStackMapping.Values
            .Distinct()
            .OrderBy(s => s)
            .Select(stackName => new
            {
                StackName = stackName,
                AdIds = adStackMapping.Where(kv => kv.Value == stackName).Select(kv => kv.Key).ToHashSet()
            })
            .Select(x => new
            {
                x.StackName,
                Counts = GetMonthlyCountsFromSalaries(salaries.Where(s => x.AdIds.Contains(s.AdId)).ToList())
            })
            .Where(x => x.Counts.Count > 0)
            .ToDictionary(x => x.StackName, x => x.Counts);

        return new Report("report.ads.monthly_by_year", baseResults, seriesOverlays: seriesOverlays.Count > 0 ? seriesOverlays : null);
    }


    private static Dictionary<string, double> GetMonthlyCountsFromSalaries(List<SalaryEntity> salaries)
        => salaries
            .GroupBy(s => s.Date.Year)
            .Select(yearGroup => new
            {
                AdsByMonth = yearGroup
                    .GroupBy(g => g.Date.Month)
                    .Select(g => new { g.Key, Count = g.Count(), Year = yearGroup.Key })
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => new { g.Count, Month = g.Key, g.Year })
            })
            .SelectMany(x => x.AdsByMonth)
            .OrderBy(x => x.Value.Year)
            .ThenBy(x => x.Value.Month)
            .ToDictionary(pair => pair.Value.Year + " " + pair.Value.Month.ToString("00"), pair => (double) pair.Value.Count);


    private static Report GetMonthlyAdCounts(List<SalaryEntity> salaries)
    {
        var results = salaries
            .GroupBy(salary => salary.Date.Month)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key.ToString("00"), group => (double) group.Count); // key: "MM"

        return new Report("report.ads.month_distribution", results, ChartType.PolarArea);
    }


    private static Report GetYearlyAdCounts(List<SalaryEntity> salaries)
    {
        var results = salaries
            .GroupBy(salary => salary.Date.Year)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key.ToString(), group => (double) group.Count);

        return new Report("report.ads.yearly_counts", results);
    }
}
