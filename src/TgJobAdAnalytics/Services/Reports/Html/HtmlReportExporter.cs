using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Enums;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Services.Reports.Html.Scriban;
using TgJobAdAnalytics.Services.Reports.Metadata;
using TgJobAdAnalytics.Models.Reports.Metadata;
using TgJobAdAnalytics.Services.Localization;
using TgJobAdAnalytics.Models.Levels.Enums;
using System.Text.RegularExpressions;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class HtmlReportExporter : IReportExporter
{
    public HtmlReportExporter(ApplicationDbContext dbContext,
        IOptions<ReportPrinterOptions> options,
        MetadataBuilder metadataBuilder,
        IOptions<SiteMetadataOptions> siteMetadataOptions,
        ILocalizationProvider localizationProvider)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _metadataBuilder = metadataBuilder;
        _siteMetadata = siteMetadataOptions.Value;
        _localization = localizationProvider;
        _templateRenderer = new TemplateRenderer(_options.TemplatePath);
    }


    public void Write(IEnumerable<ReportGroup> reportGroups)
    {
        var groups = reportGroups.Select(BuildReportItemGroup)
            .ToList();

        GenerateReports(groups);
    }


    public void Write(IEnumerable<Report> reports)
    {
        var group = new ReportItemGroup(string.Empty, reports.Select(BuildReportItem).ToList());
        GenerateReports([group]);
    }


    public void Write(Report report)
        => Write([report]);


    private List<DataSourceModel> BuildDataSourceModels()
    {
        var dates = _dbContext.Messages
            .GroupBy(m => m.TelegramChatId)
            .Select(g => new
            {
                ChatId = g.Key,
                MinDate = g.Min(m => m.TelegramMessageDate)
            })
            .ToDictionary(x => x.ChatId, x => x.MinDate);

        var chats = _dbContext.Chats.ToList();

        var lastDayOfThePreviousMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);

        var messageCounts = _dbContext.Messages
            .Where(m => DateOnly.FromDateTime(m.TelegramMessageDate) <= lastDayOfThePreviousMonth)
            .GroupBy(m => m.TelegramChatId)
            .Select(g => new { ChatId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.ChatId, g => g.Count);

        var salaryCounts = _dbContext.Salaries
            .Where(s => s.Date <= lastDayOfThePreviousMonth)
            .Join(_dbContext.Ads,
                salary => salary.AdId,
                ad => ad.Id,
                (salary, ad) => new { salary, ad })
            .Join(_dbContext.Messages,
                sa => sa.ad.MessageId,
                message => message.Id,
                (sa, message) => new { sa.salary, message.TelegramChatId })
            .GroupBy(x => x.TelegramChatId)
            .Select(g => new { ChatId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.ChatId, g => g.Count);

        List<DataSourceModel> results = [];
        foreach (var chat in chats)
        {
            if (!dates.TryGetValue(chat.TelegramId, out var minDate))
                continue;

            var processedMessages = messageCounts.TryGetValue(chat.TelegramId, out var mc) ? mc : 0;
            var extractedSalaries = salaryCounts.TryGetValue(chat.TelegramId, out var sc) ? sc : 0;

            results.Add(new DataSourceModel(id: chat.TelegramId, name: chat.Name, minimalDate: DateOnly.FromDateTime(minDate), maximalDate: lastDayOfThePreviousMonth, processedMessages: processedMessages, extractedSalaries: extractedSalaries));
        }

        return results;
    }


    private static ReportItem BuildReportItem(Report report)
    {
        var results = report.Results
            .Select(kv => new KeyValuePair<string, string>(kv.Key, FormatNumericalValue(kv.Value)))
            .ToList();

        ChartModel? chart = null;
        if (report.Type is not ChartType.None)
            chart = ChartBuilder.Build(report);

        Dictionary<string, ChartModel.DataModel>? variants = null;
        if (report.Variants is not null && report.Variants.Count > 0)
        {
            variants = new Dictionary<string, ChartModel.DataModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, data) in report.Variants)
            {
                var dm = ChartBuilder.BuildData(label: report.Title + " — " + name, results: data);
                variants[name] = dm;
            }
        }

        return new(code: report.Title, title: report.Title, results: results, chart: chart, variants: variants);
    }


    private static ReportItemGroup BuildReportItemGroup(ReportGroup reportGroup)
        => new(reportGroup.Title, [.. reportGroup.Reports.Select(BuildReportItem)]);


    private void GenerateReports(List<ReportItemGroup> reportItemGroups)
    {
        var dataSources = BuildDataSourceModels();
        var generationTime = DateTime.UtcNow;
        var runFolderName = generationTime.ToString("yyyyMMdd-HHmmss'Z'");
        var runRoot = Path.Combine(_options.OutputPath, runFolderName);

        foreach (var locale in _siteMetadata.Locales)
        {
            var localizedGroups = LocalizeGroups(reportItemGroups, locale);
            var persistedPublishedUtc = ReadPublishedTimestamp(locale);
            var metadata = _metadataBuilder.Build(locale: locale, kpis: null, persistedPublishedUtc: persistedPublishedUtc, generatedUtc: generationTime);
            var localizationDict = BuildLocalizationDictionary(locale);
            localizationDict["_dump"] = JsonSerializer.Serialize(localizationDict);
            var reportModel = ReportModelBuilder.Build(localizedGroups, dataSources, metadata, localizationDict);
            var html = _templateRenderer.Render(reportModel);

            WriteToFile(html, locale, runRoot);
            PersistPublishedTimestamp(locale, metadata.PublishedUtc);
        }
    }


    private List<ReportItemGroup> LocalizeGroups(List<ReportItemGroup> groups, string locale)
    {
        var localized = new List<ReportItemGroup>(groups.Count);
        foreach (var g in groups)
        {
            var localizedReports = new List<ReportItem>(g.Reports.Count);
            foreach (var r in g.Reports)
            {
                var localizedTitle = _localization.Get(locale, r.Code);

                Dictionary<string, ChartModel.DataModel>? localizedVariants = null;
                if (r.Variants is not null)
                {
                    localizedVariants = new Dictionary<string, ChartModel.DataModel>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in r.Variants)
                    {
                        var variantKey = kv.Key;
                        var localizedVariantKey = LocalizeVariantKey(locale, variantKey);

                        // Localize labels inside variant dataset
                        var localizedVariantLabels = kv.Value.Labels.Select(l => LocalizeMonthKey(locale, l)).ToList();
                        var origDataset = kv.Value.Dataset;
                        var localizedVariantDataset = new ChartModel.DatasetModel(
                            label: localizedTitle + " — " + localizedVariantKey,
                            data: origDataset.Data,
                            backgroundColor: origDataset.BackgroundColor,
                            borderColor: origDataset.BorderColor);
                        var localizedVariantDataModel = new ChartModel.DataModel(localizedVariantLabels, localizedVariantDataset);
                        localizedVariants[localizedVariantKey] = localizedVariantDataModel;
                    }
                }

                var localizedResults = r.Results
                    .Select(kv => new KeyValuePair<string, string>(LocalizeMonthKey(locale, kv.Key), kv.Value))
                    .ToList();

                ChartModel? localizedChart = null;
                if (r.Chart is ChartModel chart)
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

                localizedReports.Add(new ReportItem(r.Code, localizedTitle, localizedResults, localizedChart, localizedVariants));
            }

            var groupTitle = _localization.Get(locale, g.Title);
            localized.Add(new ReportItemGroup(groupTitle, localizedReports));
        }

        return localized;
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
            return monthDigits;

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

        if (key is null)
            return monthDigits;

        try { return _localization.Get(locale, key); } catch { return monthDigits; }
    }


    private string LocalizeVariantKey(string locale, string key)
    {
        if (string.Equals(key, "Все", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "All", StringComparison.OrdinalIgnoreCase))
        {
            try { return _localization.Get(locale, "variant.all"); } catch { return key; }
        }

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
            {
                try { return _localization.Get(locale, mapKey); } catch { }
            }
        }

        return key;
    }


    private static string FormatNumericalValue(double value)
        => value % 1 == 0
            ? value.ToString("N0")
            : value.ToString("N2");


    private void WriteToFile(string content, string locale, string runRoot)
    {
        var path = Path.Combine(runRoot, locale, EvergreenFileName);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
    }


    private DateTime? ReadPublishedTimestamp(string locale)
    {
        try
        {
            var sidecarPath = GetSidecarPath(locale);
            if (!File.Exists(sidecarPath))
                return null;

            var json = File.ReadAllText(sidecarPath);
            var model = JsonSerializer.Deserialize<PublishedSidecar>(json);
            return model?.PublishedUtc;
        }
        catch
        {
            return null;
        }
    }


    private void PersistPublishedTimestamp(string locale, DateTime publishedUtc)
    {
        var sidecarPath = GetSidecarPath(locale);
        var directory = Path.GetDirectoryName(sidecarPath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(new PublishedSidecar(publishedUtc));
        File.WriteAllText(sidecarPath, json);
    }


    private string GetSidecarPath(string locale)
        => Path.Combine(_options.OutputPath, "stable", locale, PublishedSidecarFileName);


    private Dictionary<string, object> BuildLocalizationDictionary(string locale)
    {
        Dictionary<string, object> uiRoot = new(StringComparer.OrdinalIgnoreCase);

        string[] uiKeys =
        [
            "ui.updated",
            "ui.button.show_table",
            "ui.button.hide_table",
            "ui.data_sources.title",
            "ui.data_sources.messages_label",
            "ui.data_sources.salaries_label",
            "ui.data_sources.explainer",
            "ui.footer.author",
            "ui.footer.source",
            "ui.footer.built_with",
            "ui.footer.and",
            "ui.chart.position_label"
        ];

        foreach (var fullKey in uiKeys)
        {
            var path = fullKey.Substring(3).Split('.');
            Dictionary<string, object> current = uiRoot;
            for (int i = 0; i < path.Length; i++)
            {
                var segment = path[i];
                var isLeaf = i == path.Length - 1;

                if (isLeaf)
                {
                    string localizedValue;
                    try { localizedValue = _localization.Get(locale, fullKey); }
                    catch { localizedValue = fullKey; }
                    current[segment] = localizedValue;
                }
                else
                {
                    if (!current.TryGetValue(segment, out var next) || next is not Dictionary<string, object> nextDict)
                    {
                        nextDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        current[segment] = nextDict;
                    }
                    current = nextDict;
                }
            }
        }

        var variantRoot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        try { variantRoot["all"] = _localization.Get(locale, "variant.all"); } catch { variantRoot["all"] = "All"; }

        var levelRoot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string[] levelKeys =
        [
            "level.intern",
            "level.junior",
            "level.middle",
            "level.senior",
            "level.lead",
            "level.architect",
            "level.manager"
        ];
        foreach (var lk in levelKeys)
        {
            var shortKey = lk.Split('.')[1];
            try { levelRoot[shortKey] = _localization.Get(locale, lk); } catch { levelRoot[shortKey] = shortKey; }
        }

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ui"] = uiRoot,
            ["variant"] = variantRoot,
            ["level"] = levelRoot
        };
    }


    private const string EvergreenFileName = "index.html";
    private const string PublishedSidecarFileName = ".published.json";

    private readonly ApplicationDbContext _dbContext;
    private readonly ReportPrinterOptions _options;
    private readonly TemplateRenderer _templateRenderer;
    private readonly MetadataBuilder _metadataBuilder;
    private readonly SiteMetadataOptions _siteMetadata;
    private readonly ILocalizationProvider _localization;
    private static readonly Regex _yearMonthRegex = new("^(\\d{4}) (\\d{2})$", RegexOptions.Compiled);
    private static readonly Regex _monthOnlyRegex = new("^(\\d{2})$", RegexOptions.Compiled);

    private sealed record PublishedSidecar(DateTime PublishedUtc);
}
