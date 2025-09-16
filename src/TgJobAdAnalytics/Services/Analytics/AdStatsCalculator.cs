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

        return new ReportGroup("Статистика по вакансиям", reports);
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
            .ToDictionary(group => group.Year + " " + new DateTime(1, group.Month, 1).ToString("MMMM"), group => (double) group.Count);

        return new Report("Топ-3 лучших месяца по предложениям за всё время", results, ChartType.None);
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
            .ToDictionary(pair => pair.Value.Year + " " + new DateTime(1, pair.Value.Month, 1).ToString("MMMM"), pair => (double) pair.Value.Count);

        return new Report("Количество вакансий по месяцам", results);
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
            .ToDictionary(group => new DateTime(1, group.Key, 1).ToString("MMMM"), group => (double) group.Count);

        return new Report("Общее количество вакансий по месяцам года", results, ChartType.PolarArea);
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

        return new Report("Общее количество вакансий по годам", results);
    }
}
