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

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class HtmlReportExporter : IReportExporter
{
    public HtmlReportExporter(ApplicationDbContext dbContext, IOptions<ReportPrinterOptions> options, MetadataBuilder metadataBuilder, IOptions<SiteMetadataOptions> siteMetadataOptions, ILocalizationProvider localizationProvider)
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
        foreach (var chat in chats) {
            if (!dates.TryGetValue(chat.TelegramId, out var minDate))
                continue;

            var processedMessages = messageCounts.TryGetValue(chat.TelegramId, out var mc) ? mc : 0;
            var extractedSalaries = salaryCounts.TryGetValue(chat.TelegramId, out var sc) ? sc : 0;

            // Trim dates to the last day of the previous month to avoid showing incomplete interval
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

        return new(title: report.Title, results: results, chart: chart, variants: variants);
    }


    private static ReportItemGroup BuildReportItemGroup(ReportGroup reportGroup)
        => new(reportGroup.Title, [.. reportGroup.Reports.Select(BuildReportItem)]);


    private void GenerateReports(List<ReportItemGroup> reportItemGroups)
    {
        var dataSources = BuildDataSourceModels();
        var generationTime = DateTime.UtcNow;

        foreach (var locale in _siteMetadata.Locales)
        {
            var localizedGroups = LocalizeGroups(reportItemGroups, locale);
            var persistedPublishedUtc = ReadPublishedTimestamp(locale);
            var metadata = _metadataBuilder.Build(locale: locale, kpis: null, persistedPublishedUtc: persistedPublishedUtc, generatedUtc: generationTime);
            var reportModel = ReportModelBuilder.Build(localizedGroups, dataSources, metadata);
            var html = _templateRenderer.Render(reportModel);

            WriteToFile(html, locale);
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
                var title = _localization.Get(locale, r.Title);
                localizedReports.Add(new ReportItem(title, r.Results, r.Chart, r.Variants));
            }

            var groupTitle = _localization.Get(locale, g.Title);
            localized.Add(new ReportItemGroup(groupTitle, localizedReports));
        }

        return localized;
    }


    private static string FormatNumericalValue(double value)
        => value % 1 == 0 
            ? value.ToString("N0") 
            : value.ToString("N2");


    private void WriteToFile(string content, string locale)
    {
        var path = Path.Combine(_options.OutputPath, locale, "reports", EvergreenFileName);
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
            return null; // On error treat as first publish
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
        => Path.Combine(_options.OutputPath, locale, "reports", PublishedSidecarFileName);


    private const string EvergreenFileName = "index.html";
    private const string PublishedSidecarFileName = ".published.json";

    private readonly ApplicationDbContext _dbContext;
    private readonly ReportPrinterOptions _options;
    private readonly TemplateRenderer _templateRenderer;
    private readonly MetadataBuilder _metadataBuilder;
    private readonly SiteMetadataOptions _siteMetadata;
    private readonly ILocalizationProvider _localization;

    private sealed record PublishedSidecar(DateTime PublishedUtc);
}
