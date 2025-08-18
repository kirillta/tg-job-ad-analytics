using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Reports.Html.Scriban;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class HtmlReportPrinter : IReportPrinter
{
    public HtmlReportPrinter(ApplicationDbContext dbContext, IOptions<ReportPrinterOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;

        _templateRenderer = new TemplateRenderer(_options.TemplatePath);
    }


    public void Print(IEnumerable<ReportGroup> reportGroups)
    {
        var groups = reportGroups.Select(BuildReportItemGroup)
            .ToList();

        GenerateReport(groups);
    }


    public void Print(IEnumerable<Report> reports)
    {
        var group = new ReportItemGroup(string.Empty, reports.Select(BuildReportItem).ToList());
        GenerateReport([group]);
    }


    public void Print(Report report) 
        => Print([report]);


    private List<DataSourceModel> BuildDataSourceModels()
    {
        // get min and max dates for each chat from Messages table as a dictionary <chatId, (minDate, maxDate)>
        var dates = _dbContext.Messages
            .GroupBy(m => m.TelegramChatId)
            .Select(g => new
            {
                ChatId = g.Key,
                MinDate = g.Min(m => m.TelegramMessageDate),
                MaxDate = g.Max(m => m.TelegramMessageDate)
            })
            .ToDictionary(x => x.ChatId, x => (x.MinDate, x.MaxDate));

        var chats = _dbContext.Chats
            .ToList();

        List<DataSourceModel> results = [];
        foreach (var chat in chats) {
            if (!dates.TryGetValue(chat.TelegramId, out var dateRange))
                continue;

            var minDate = DateOnly.FromDateTime(dateRange.MinDate);
            var maxDate = DateOnly.FromDateTime(dateRange.MaxDate);

            results.Add(new DataSourceModel(chat.TelegramId, chat.Name, minDate, maxDate));
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

        return new(report.Title, results, chart);
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
