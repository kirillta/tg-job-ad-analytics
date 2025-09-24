using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Services.Localization;
using TgJobAdAnalytics.Models.Levels.Enums;
using System.Text.RegularExpressions;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class ReportGroupLocalizer
{ 
    public ReportGroupLocalizer(ILocalizationProvider localizationProvider)
    {
        _localizationProvider = localizationProvider;
    }


    public List<ReportItemGroup> Localize(List<ReportItemGroup> groups, string locale)
    {
        var localized = new List<ReportItemGroup>(groups.Count);
        foreach (var group in groups)
        {
            var localizedReports = new List<ReportItem>(group.Reports.Count);
            foreach (var report in group.Reports)
            {
                var localizedTitle = _localizationProvider.Get(locale, report.Code);

                Dictionary<string, ChartModel.DataModel>? localizedVariants = null;
                if (report.Variants is not null)
                {
                    localizedVariants = new Dictionary<string, ChartModel.DataModel>(StringComparer.OrdinalIgnoreCase);
                    foreach (var variant in report.Variants)
                    {
                        var variantKey = variant.Key;
                        var localizedVariantKey = LocalizeVariantKey(locale, variantKey);

                        var localizedVariantLabels = variant.Value.Labels.Select(l => LocalizeMonthKey(locale, l)).ToList();
                        var origDataset = variant.Value.Dataset;

                        var localizedVariantDataset = new ChartModel.DatasetModel(
                            label: localizedTitle + " — " + localizedVariantKey,
                            data: origDataset.Data,
                            backgroundColor: origDataset.BackgroundColor,
                            borderColor: origDataset.BorderColor
                        );
                        var localizedVariantDataModel = new ChartModel.DataModel(localizedVariantLabels, localizedVariantDataset);

                        localizedVariants[localizedVariantKey] = localizedVariantDataModel;
                    }
                }

                var localizedResults = report.Results
                    .Select(kv => new KeyValuePair<string, string>(LocalizeMonthKey(locale, kv.Key), kv.Value))
                    .ToList();

                ChartModel? localizedChart = null;
                if (report.Chart is ChartModel chart)
                {
                    var localizedLabels = chart.Data.Labels.Select(l => LocalizeMonthKey(locale, l)).ToList();
                    var orig = chart.Data.Dataset;
                    var localizedDataset = new ChartModel.DatasetModel(
                        label: localizedTitle,
                        data: orig.Data,
                        backgroundColor: orig.BackgroundColor,
                        borderColor: orig.BorderColor);
                    var localizedData = new ChartModel.DataModel(localizedLabels, localizedDataset);

                    localizedChart = new ChartModel(chart.Id, chart.Type, localizedData);
                }

                localizedReports.Add(new ReportItem(report.Code, localizedTitle, localizedResults, localizedChart, localizedVariants));
            }

            var groupTitle = _localizationProvider.Get(locale, group.Title);
            localized.Add(new ReportItemGroup(groupTitle, localizedReports));
        }

        return localized;
    }


    private string LocalizeVariantKey(string locale, string key)
    {
        if (string.Equals(key, "Все", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "All", StringComparison.OrdinalIgnoreCase))
            return _localizationProvider.Get(locale, "variant.all");

        if (Enum.TryParse<PositionLevel>(key, ignoreCase: true, out var level))
        {
            var mapKey = level switch
            {
                PositionLevel.Intern => "level.intern",
                PositionLevel.Junior => "level.junior",
                PositionLevel.Middle => "level.middle",
                PositionLevel.Senior => "level.senior",
                PositionLevel.Lead => "level.lead",
                PositionLevel.Architect => "level.architect",
                PositionLevel.Manager => "level.manager",
                _ => null
            };

            if (mapKey is not null)
                return _localizationProvider.Get(locale, mapKey);
        }

        return key;
    }


    private string LocalizeMonthKey(string locale, string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return rawKey;

        var yearMonthMatch = _yearMonthRegex.Match(rawKey);
        if (yearMonthMatch.Success)
        {
            var year = yearMonthMatch.Groups[1].Value;
            var monthDigits = yearMonthMatch.Groups[2].Value;
            var monthName = ResolveMonth(locale, monthDigits);

            return year + " " + monthName;
        }

        var monthOnlyMatch = _monthOnlyRegex.Match(rawKey);
        if (monthOnlyMatch.Success)
        {
            var monthDigits = monthOnlyMatch.Groups[1].Value;
            return ResolveMonth(locale, monthDigits);
        }

        return rawKey;
    }


    private string ResolveMonth(string locale, string monthDigits)
    {
        if (!int.TryParse(monthDigits, out var m) || m < 1 || m > 12)
            throw new ArgumentOutOfRangeException(nameof(monthDigits), "Month must be between 01 and 12");

        var key = m switch
        {
            1 => "month.january",
            2 => "month.february",
            3 => "month.march",
            4 => "month.april",
            5 => "month.may",
            6 => "month.june",
            7 => "month.july",
            8 => "month.august",
            9 => "month.september",
            10 => "month.october",
            11 => "month.november",
            12 => "month.december",
            _ => null
        };

        return _localizationProvider.Get(locale, key!); 
    }

    
    private static readonly Regex _yearMonthRegex = new("^(\\d{4}) (\\d{2})$", RegexOptions.Compiled);
    private static readonly Regex _monthOnlyRegex = new("^(\\d{2})$", RegexOptions.Compiled);

    private readonly ILocalizationProvider _localizationProvider;
}
