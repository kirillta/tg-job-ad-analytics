using Scriban;
using Scriban.Runtime;
using System.Text;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Reports;

public sealed class HtmlReportPrinter : IReportPrinter
{
    public HtmlReportPrinter(string path, List<TgChat> dataSources)
    {
        _dataSources = dataSources;
        _path = path;
    }


    public void Print(IEnumerable<ReportGroup> reportGroups)
    {
        var body = new StringBuilder();
        foreach (var reportGroup in reportGroups)
            body.Append(PrintGroupInternal(reportGroup));

        FinalizeReport(body.ToString());
    }


    public void Print(IEnumerable<Report> reports)
    {
        var body = new StringBuilder();
        foreach (Report report in reports) 
        {
            body.Append(PrintInternal(report));
            body.AppendLine("<hr>");
        }

        FinalizeReport(body.ToString());
    }


    public void Print(Report report)
    {
        var body = PrintInternal(report);
        FinalizeReport(body);
    }


    private void FinalizeReport(string body)
    {
        var templateContent = File.ReadAllText("Views/Reports/ReportTemplate.sbn");
        var template = Template.Parse(templateContent);

        var reportModel = BuildReportModel();

        var scriptObject = new ScriptObject();
        scriptObject.Import(reportModel);

        var context = new TemplateContext();
        context.TemplateLoader = new FileSystemLoader("");
        context.PushGlobal(scriptObject);

        var html = template.Render(context);
        WriteToFile(html);


        List<DataSourceModel> BuildDataSourceModels()
            => _dataSources.Select(source => new DataSourceModel
            {
                Id = source.Id,
                Name = source.Name,
                MinimalDate = source.Messages.Min(m => m.Date).ToString("yyyy.MM.dd"),
                MaximalDate = source.Messages.Max(m => m.Date).ToString("yyyy.MM.dd")
            }).ToList();


        ReportModel BuildReportModel()
            => new()
            {
                Body = body,
                DataSources = BuildDataSourceModels(),
                ReportDate = DateTime.UtcNow.ToString("yyyy.MM.dd")
            };
    }


    private static string FormatNumericalValue(double value) 
        => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");


    private static string PrintGroupInternal(ReportGroup reportGroup)
    {
        var body = new StringBuilder();

        body.AppendLine("<section class=\"mb-10\">");
        body.AppendLine($"<h2 class=\"text-2xl font-bold text-center\">{reportGroup.Title}</h2>");
        foreach (var report in reportGroup.Reports)
            body.Append(PrintInternal(report));
        body.AppendLine("</section>");

        return body.ToString();
    }


    private static string PrintInternal(Report report)
    {
        var section = new StringBuilder();

        section.AppendLine("<div class=\"mb-5\">");
        section.AppendLine("<table class=\"min-w-80 table-auto border-collapse border border-gray-200\">");
        section.AppendLine($"<caption class=\"caption-top text-lg font-semibold mb-2\">{report.Title}</caption>");
        foreach (var result in report.Results)
        {
            section.AppendLine("<tr>");
            section.AppendLine($"<td class=\"border border-gray-300 px-4 py-2 min-w-[15vw]\">{result.Key}</td>");
            section.AppendLine($"<td class=\"border border-gray-300 px-4 py-2 min-w-[15vw] text-right\">{FormatNumericalValue(result.Value)}</td>");
            section.AppendLine("</tr>");
        }
        section.AppendLine("</table>");

        if (report.Type != ChartType.None)
            section.Append(ChartBuilder.Build(report));

        section.AppendLine("</div>");

        return section.ToString();
    }


    private void WriteToFile(string content)
    {
        var fileName = string.Format(ResultsFileNameTemplate, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        var path = Path.Combine(_path, fileName);
        if (!File.Exists(path))
        {
            if (!string.IsNullOrEmpty(_path))
                Directory.CreateDirectory(_path);
        }

        File.WriteAllText(path, content);
    }


    private const string ResultsFileNameTemplate = "{0}-report.html";

    private readonly List<TgChat> _dataSources;
    private readonly string _path;
}
