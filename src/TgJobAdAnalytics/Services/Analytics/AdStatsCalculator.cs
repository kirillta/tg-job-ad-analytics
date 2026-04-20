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

        var adIdsByStack = adStackMapping
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToHashSet());

        var seriesOverlays = adIdsByStack
            .OrderBy(kv => kv.Key)
            .Select(kv => new
            {
                StackName = kv.Key,
                Counts = GetMonthlyCountsFromSalaries(salaries.Where(s => kv.Value.Contains(s.AdId)).ToList())
            })
            .Where(x => x.Counts.Count > 0)
            .ToDictionary(x => x.StackName, x => x.Counts);

        return new Report("report.ads.monthly_by_year", baseResults, seriesOverlays: seriesOverlays.Count > 0 ? seriesOverlays : null);
    }


    private static Dictionary<string, double> GetMonthlyCountsFromSalaries(List<SalaryEntity> salaries)
        => salaries
            .GroupBy(s => new { s.Date.Year, s.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToDictionary(
                g => g.Key.Year + " " + g.Key.Month.ToString("00"),
                g => (double) g.Count());


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
        var allLocations = new[] 
        { 
            VacancyLocation.Russia, 
            VacancyLocation.Belarus, 
            VacancyLocation.Cis, 
            VacancyLocation.Europe, 
            VacancyLocation.Us, 
            VacancyLocation.MiddleEast,
            VacancyLocation.Other
        };

        return GetRatioReport("report.ads.location_ratio", salaries, adLocationMapping, allLocations, LocationLabel);
    }


    private static Report GetWorkFormatRatio(List<SalaryEntity> salaries, Dictionary<Guid, WorkFormat> adWorkFormatMapping)
    {
        var allFormats = new[] 
        { 
            WorkFormat.OnSite, 
            WorkFormat.Hybrid, 
            WorkFormat.RemoteDomestic,
            WorkFormat.RemoteWorldwide
        };

        return GetRatioReport("report.ads.work_format_ratio", salaries, adWorkFormatMapping, allFormats, FormatLabel);
    }


    private static Report GetRatioReport<TEnum>(
        string reportKey,
        List<SalaryEntity> salaries,
        Dictionary<Guid, TEnum> mapping,
        TEnum[] allValues,
        Func<TEnum, string> labelSelector) where TEnum : struct, Enum
    {
        var primaryValue = allValues[0];

        var monthlyGroups = salaries
            .Where(s => mapping.ContainsKey(s.AdId))
            .GroupBy(s => s.Date.Year + " " + s.Date.Month.ToString("00"))
            .OrderBy(g => g.Key)
            .ToList();

        var primaryData = new Dictionary<string, double>();
        var overlayData = allValues.Skip(1).ToDictionary(v => labelSelector(v), _ => new Dictionary<string, double>());

        foreach (var group in monthlyGroups)
            ProcessMonth(group);

        var seriesOverlays = overlayData.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        return new Report(reportKey, primaryData, ChartType.StackedBar, seriesOverlays: seriesOverlays.Count > 0 ? seriesOverlays : null, primarySeriesLabel: labelSelector(primaryValue));


        void ProcessMonth(IGrouping<string, SalaryEntity> group)
        {
            var total = group.Count();
            if (total == 0)
                return;

            var countByValue = group.GroupBy(s => mapping[s.AdId]).ToDictionary(g => g.Key, g => g.Count());

            foreach (var value in allValues)
            {
                countByValue.TryGetValue(value, out var count);
                var percentage = Math.Round(count / (double) total * 100, 1);

                if (EqualityComparer<TEnum>.Default.Equals(value, primaryValue))
                    primaryData[group.Key] = percentage;
                else
                    overlayData[labelSelector(value)][group.Key] = percentage;
            }
        }
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
