using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Enums;

namespace TgJobAdAnalytics.Services.Analytics;

public sealed class AdStatsCalculator
{
    public static ReportGroup GenerateAll(List<SalaryEntity> salaries)
    {
        var reports = new List<Report>
        {
            GetNumberOfAdsByYearAndMonth(salaries),
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


    private static Report GetNumberOfAdsByYearAndMonth(List<SalaryEntity> salaries)
    {
        var results = salaries
            .GroupBy(salary => salary.Date.Year)
            .Select(yearGroup => new
            {
                AdsByMonth = yearGroup
                    .GroupBy(group => group.Date.Month)
                    .Select(group => new
                    {
                        group.Key,
                        Count = group.Count(),
                        Year = yearGroup.Key,
                    })
                    .OrderBy(group => group.Key)
                    .ToDictionary(group => group.Key, group => new
                    {
                        group.Count,
                        Month = group.Key,
                        group.Year,
                    })
            })
            .SelectMany(group => group.AdsByMonth)
            .OrderBy(group => group.Value.Year)
            .ThenBy(group => group.Value.Month)
            .ToDictionary(pair => pair.Value.Year + " " + pair.Value.Month.ToString("00"), pair => (double) pair.Value.Count); // key: "YYYY MM"

        return new Report("report.ads.monthly_by_year", results);
    }


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
