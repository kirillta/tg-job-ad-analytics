using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Reports.Html.Scriban;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class HtmlReportPrinter : IReportPrinter
{
    public HtmlReportPrinter(string outputPath, string templatesPath, List<TgChat> dataSources)
    {
        _dataSources = dataSources;
        _outputPath = outputPath;
        _templatesPath = templatesPath;

        _templateRenderer = new TemplateRenderer(_templatesPath);
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


    private static ReportItem BuildReportItem(Report report)
    {
        var results = report.Results
                    .Select(kv => new KeyValuePair<string, string>(kv.Key, FormatNumericalValue(kv.Value)))
                    .ToList();

        return new(report.Title, results);
    }


    private static ReportItemGroup BuildReportItemGroup(ReportGroup reportGroup)
        => new(reportGroup.Title, reportGroup.Reports.Select(BuildReportItem).ToList());


    private void GenerateReport(List<ReportItemGroup> reportItemGroups)
    {
        var reportModel = ReportModelBuilder.Build(reportItemGroups, _dataSources);
        var html = _templateRenderer.Render(reportModel);
        WriteToFile(html);
    }


    private static string FormatNumericalValue(double value)
        => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");


    private void WriteToFile(string content)
    {
        var fileName = string.Format(ResultsFileNameTemplate, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        var path = Path.Combine(_outputPath, fileName);
        if (!File.Exists(path))
        {
            if (!string.IsNullOrEmpty(_outputPath))
                Directory.CreateDirectory(_outputPath);
        }

        File.WriteAllText(path, content);
    }


    private const string ResultsFileNameTemplate = "{0}-report.html";

    private readonly List<TgChat> _dataSources;
    private readonly string _outputPath;
    private readonly string _templatesPath;
    private readonly TemplateRenderer _templateRenderer;
}
