using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Locations.Enums;
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
    /// (top months, monthly distribution, yearly counts, location ratio, work format ratio).
    /// </summary>
    /// <param name="salaries">Collection of salary entities extracted from advertisements.</param>
    /// <param name="adStackMapping">Mapping of ad identifiers to their technology stack names.</param>
    /// <param name="adLocationMapping">Mapping of ad identifiers to their vacancy location classification.</param>
    /// <param name="adWorkFormatMapping">Mapping of ad identifiers to their work format classification.</param>
    /// <returns>A <see cref="ReportGroup"/> containing the computed advertisement statistics reports.</returns>
    public static ReportGroup GenerateAll(
        List<SalaryEntity> salaries,
        Dictionary<Guid, string> adStackMapping,
        Dictionary<Guid, VacancyLocation> adLocationMapping,
        Dictionary<Guid, WorkFormat> adWorkFormatMapping)
    {
        var reports = new List<Report>
        {
            GetNumberOfAdsByYearAndMonth(salaries, adStackMapping),
            GetTopMonthsByAdCount(salaries),
            GetMonthlyAdCounts(salaries),
            GetYearlyAdCounts(salaries),
            GetLocationRatio(salaries, adLocationMapping),
            GetWorkFormatRatio(salaries, adWorkFormatMapping),
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


    private static Report GetLocationRatio(List<SalaryEntity> salaries, Dictionary<Guid, VacancyLocation> adLocationMapping)
    {
        var monthlyGroups = salaries
            .Where(s => adLocationMapping.ContainsKey(s.AdId))
            .GroupBy(s => s.Date.Year + " " + s.Date.Month.ToString("00"))
            .OrderBy(g => g.Key)
            .ToList();

        var allLocations = new[] { VacancyLocation.Russia, VacancyLocation.Belarus, VacancyLocation.Cis, VacancyLocation.Europe, VacancyLocation.Us, VacancyLocation.MiddleEast, VacancyLocation.Other };

        var russiaData = new Dictionary<string, double>();
        var overlayData = allLocations.Skip(1).ToDictionary(loc => LocationLabel(loc), _ => new Dictionary<string, double>());

        foreach (var group in monthlyGroups)
        {
            var monthKey = group.Key;
            var total = group.Count();
            if (total == 0)
                continue;

            foreach (var loc in allLocations)
            {
                var count = group.Count(s => adLocationMapping.TryGetValue(s.AdId, out var l) && l == loc);
                var percentage = Math.Round(count / (double)total * 100, 1);

                if (loc == VacancyLocation.Russia)
                    russiaData[monthKey] = percentage;
                else
                    overlayData[LocationLabel(loc)][monthKey] = percentage;
            }
        }

        var seriesOverlays = overlayData.Where(kv => kv.Value.Count > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return new Report("report.ads.location_ratio", russiaData, ChartType.StackedBar, seriesOverlays: seriesOverlays.Count > 0 ? seriesOverlays : null);
    }


    private static Report GetWorkFormatRatio(List<SalaryEntity> salaries, Dictionary<Guid, WorkFormat> adWorkFormatMapping)
    {
        var monthlyGroups = salaries
            .Where(s => adWorkFormatMapping.ContainsKey(s.AdId))
            .GroupBy(s => s.Date.Year + " " + s.Date.Month.ToString("00"))
            .OrderBy(g => g.Key)
            .ToList();

        var allFormats = new[] { WorkFormat.OnSite, WorkFormat.Hybrid, WorkFormat.RemoteDomestic, WorkFormat.RemoteWorldwide };

        var onSiteData = new Dictionary<string, double>();
        var overlayData = allFormats.Skip(1).ToDictionary(fmt => FormatLabel(fmt), _ => new Dictionary<string, double>());

        foreach (var group in monthlyGroups)
        {
            var monthKey = group.Key;
            var total = group.Count();
            if (total == 0)
                continue;

            foreach (var fmt in allFormats)
            {
                var count = group.Count(s => adWorkFormatMapping.TryGetValue(s.AdId, out var f) && f == fmt);
                var percentage = Math.Round(count / (double)total * 100, 1);

                if (fmt == WorkFormat.OnSite)
                    onSiteData[monthKey] = percentage;
                else
                    overlayData[FormatLabel(fmt)][monthKey] = percentage;
            }
        }

        var seriesOverlays = overlayData.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

        return new Report("report.ads.work_format_ratio", onSiteData, ChartType.StackedBar, seriesOverlays: seriesOverlays.Count > 0 ? seriesOverlays : null);
    }


    private static string LocationLabel(VacancyLocation location) 
        => location switch
        {
            VacancyLocation.Russia => "location.russia",
            VacancyLocation.Belarus => "location.belarus",
            VacancyLocation.Cis => "location.cis",
            VacancyLocation.Europe => "location.europe",
            VacancyLocation.Us => "location.us",
            VacancyLocation.MiddleEast => "location.middle_east",
            VacancyLocation.Other => "location.other",
            _ => "location.other"
        };


    private static string FormatLabel(WorkFormat format) 
        => format switch
        {
            WorkFormat.OnSite => "format.on_site",
            WorkFormat.Hybrid => "format.hybrid",
            WorkFormat.RemoteDomestic => "format.remote_domestic",
            WorkFormat.RemoteWorldwide => "format.remote_worldwide",
            _ => "format.remote_worldwide"
        };
}
