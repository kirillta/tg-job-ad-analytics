using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Services.Reports.Html.Scriban;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class HtmlReportExporter : IReportExporter
{
    public HtmlReportExporter(ApplicationDbContext dbContext, IOptions<ReportPrinterOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;

        _templateRenderer = new TemplateRenderer(_options.TemplatePath);
    }


    public void Write(IEnumerable<ReportGroup> reportGroups)
    {
        var groups = reportGroups.Select(BuildReportItemGroup)
            .ToList();

        GenerateReport(groups);
    }


    public void Write(IEnumerable<Report> reports)
    {
        var group = new ReportItemGroup(string.Empty, reports.Select(BuildReportItem).ToList());
        GenerateReport([group]);
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

        var chats = _dbContext.Chats
            .ToList();

        var lastDayOfThePreviousMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);

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

            // Because we trim dates to the last day of the previous month to avoid showing incomplete data intervals
            results.Add(new DataSourceModel(chat.TelegramId, chat.Name, DateOnly.FromDateTime(minDate), lastDayOfThePreviousMonth, processedMessages, extractedSalaries));
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
        => new(reportGroup.Title, reportGroup.Reports.Select(BuildReportItem).ToList());


    private void GenerateReport(List<ReportItemGroup> reportItemGroups)
    {
        var dataSources = BuildDataSourceModels();

        var reportModel = ReportModelBuilder.Build(reportItemGroups, dataSources);
        var html = _templateRenderer.Render(reportModel);
        WriteToFile(html);
    }


    private static string FormatNumericalValue(double value)
        => value % 1 == 0 
            ? value.ToString("N0") 
            : value.ToString("N2");


    private void WriteToFile(string content)
    {
        var fileName = string.Format(ResultsFileNameTemplate, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        var path = Path.Combine(_options.OutputPath, fileName);
        if (!File.Exists(path))
        {
            if (!string.IsNullOrEmpty(_options.OutputPath))
                Directory.CreateDirectory(_options.OutputPath);
        }

        File.WriteAllText(path, content);
    }


    private const string ResultsFileNameTemplate = "{0}-report.html";

    private readonly ApplicationDbContext _dbContext;
    private readonly ReportPrinterOptions _options;
    private readonly TemplateRenderer _templateRenderer;
}
