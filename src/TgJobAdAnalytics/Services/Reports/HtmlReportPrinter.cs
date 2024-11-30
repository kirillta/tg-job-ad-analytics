using System.Text;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports;

public sealed class HtmlReportPrinter : IReportPrinter
{
    public HtmlReportPrinter(string path)
    {
        _path = path;
    }


    public void Print(IEnumerable<ReportGroup> reportGroups)
    {
        var body = new StringBuilder();
        foreach (var reportGroup in reportGroups)
            body.Append(PrintGroupInternal(reportGroup));

        FinalizeReport(body);
    }


    public void Print(IEnumerable<Report> reports)
    {
        var body = new StringBuilder();
        foreach (Report report in reports) 
        {
            body.Append(PrintInternal(report));
            body.AppendLine("<hr>");
        }

        FinalizeReport(body);
    }


    public void Print(Report report)
    {
        var body = PrintInternal(report);
        FinalizeReport(body);
    }


    private void FinalizeReport(StringBuilder body)
    {
        var html = GenerateBaseHtml(body);
        WriteToFile(html);
    }


    private static string FormatNumericalValue(double value) 
        => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");


    private static StringBuilder GenerateBaseHtml(StringBuilder body)
    {
        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine("<title>Аналитика вакансий в Telegram</title>");
        html.AppendLine("<script src=\"https://cdn.tailwindcss.com\"></script>");
        html.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        html.AppendLine("<div class=\"container mx-auto mt-5\">");
        html.AppendLine("<h1 class=\"text-3xl font-bold text-center\">Аналитика вакансий в Telegram</h1>");
        html.Append(body);
        html.AppendLine("</div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html;
    }


    private static StringBuilder PrintGroupInternal(ReportGroup reportGroup)
    {
        var body = new StringBuilder();

        body.AppendLine("<div class=\"mb-5\">");
        
        body.AppendLine($"<h2 class=\"text-2xl font-bold text-center\">{reportGroup.Title}</h2>");
        foreach (var report in reportGroup.Reports)
            body.Append(PrintInternal(report));

        body.AppendLine("</div>");

        return body;
    }


    private static StringBuilder PrintInternal(Report report)
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

        return section;
    }


    private void WriteToFile(StringBuilder content)
    {
        var fileName = string.Format(ResultsFileNameTemplate, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        var path = Path.Combine(_path, fileName);
        if (!File.Exists(path))
        {
            if (!string.IsNullOrEmpty(_path))
                Directory.CreateDirectory(_path);
        }

        File.WriteAllText(path, content.ToString());
    }


    private const string ResultsFileNameTemplate = "{0}-report.html";

    private readonly string _path;
}
