using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services;

public sealed class AdStatsCalculator
{
    public static Report GetMaximumNumberOfAdsByMonthAndYear(List<Message> messages)
    {
        var results = messages
            .GroupBy(message => new { message.Date.Year, message.Date.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .Take(3)
            .ToDictionary(group => group.Year + " " + new DateTime(1, group.Month, 1).ToString("MMMM"), group => group.Count.ToString());

        return new Report("Maximum number of ads by month and year", results);
    }


    public static Report GetNumberOfAdsByMonth(List<Message> messages)
    {
        var results = messages
            .GroupBy(message => message.Date.Month)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .OrderBy(group => group.Key)
            .ToDictionary(group => new DateTime(1, group.Key, 1).ToString("MMMM"), group => group.Count.ToString());

        return new Report("Number of ads by month", results);
    }


    public static Report GetNumberOfAdsByYear(List<Message> messages)
    {
        var results = messages
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key.ToString(), group => group.Count.ToString());

        return new Report("Number of ads by year", results);
    }


    public static Report PrintNumberOfAdsByYearAndMonth(List<Message> messages)
    {
        var results = messages
            .GroupBy(message => message.Date.Year)
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
                        Count = group.Count.ToString(),
                        Month = group.Key,
                        group.Year,
                    })
            })
            .SelectMany(group => group.AdsByMonth)
            .OrderBy(group => group.Value.Year)
            .ThenBy(group => group.Value.Month)
            .ToDictionary(pair => pair.Value.Year + " " + new DateTime(1, pair.Value.Month, 1).ToString("MMMM"), pair => pair.Value.Count);

        return new Report("Number of ads by year and month", results);
    }
}
