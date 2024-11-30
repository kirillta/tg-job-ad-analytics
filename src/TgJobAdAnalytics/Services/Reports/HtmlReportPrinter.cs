using System.Text;
using TgJobAdAnalytics.Models.Reports;
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
        var html = GenerateBaseHtml(body, _dataSources);
        WriteToFile(html);
    }


    private static string FormatNumericalValue(double value) 
        => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");


    private static StringBuilder GenerateBaseHtml(StringBuilder body, List<TgChat> dataSources)
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

        var sources = GetSourcesSection(dataSources);
        html.Append(sources);
        html.AppendLine("</div>");

        var footer = GetFooter();
        html.Append(footer);

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html;
    }


    private static StringBuilder GetSourcesSection(List<TgChat> dataSources)
    {
        var sources = new StringBuilder();
       
        sources.AppendLine("<section class=\"mb-10\">");
        sources.AppendLine("<h5>Источники данных</h5>");
        sources.AppendLine("<ol>");
        foreach (var dataSource in dataSources)
        {
            var minimalDate = dataSource.Messages.Min(m => m.Date).ToString("yyyy.MM.dd");
            var maximalDate = dataSource.Messages.Max(m => m.Date).ToString("yyyy.MM.dd");
            sources.AppendLine("<li>");
            sources.AppendLine($"{dataSource.Name} ({dataSource.Id}), {minimalDate}–{maximalDate}.");
            sources.AppendLine("</li>");
        }
        sources.AppendLine("</ol>");
        sources.AppendLine("</section>");

        return sources;
    }


    private static StringBuilder GetFooter()
    {
        var footer = new StringBuilder();

        footer.AppendLine("<footer class=\"text-center text-sm font-light mb-10\">");

        footer.AppendLine("<p class=\"mb-15\">");
        footer.AppendLine("Автор: <a href=\"https://www.linkedin.com/in/kirillta/?locale=en_US\" class=\"text-blue-500 hover:text-blue-700 focus:text-blue-900 active:text-blue-800\">Kirill Taran</a>");
        footer.AppendLine("<br />");
        footer.AppendLine("Исходный код: <a href=\"https://github.com/kirillta/tg-job-ad-analytics\" class=\"text-blue-500 hover:text-blue-700 focus:text-blue-900 active:text-blue-800\">GitHub</a>");
        footer.AppendLine("<br />");
        footer.AppendLine($"Версия аналитики: {DateTime.UtcNow:yyyy-MM-dd}");
        footer.AppendLine("<br />");
        footer.AppendLine("Создано с использованием <a href=\"https://tailwindcss.com/\" class=\"text-blue-500 hover:text-blue-700 focus:text-blue-900 active:text-blue-800\">Tailwind CSS</a> и <a href=\"https://www.chartjs.org/\" class=\"text-blue-500 hover:text-blue-700 focus:text-blue-900 active:text-blue-800\">Chart.js</a>");
        footer.AppendLine("</p>");

        footer.AppendLine("</footer>");
        
        return footer;
    }


    private static StringBuilder PrintGroupInternal(ReportGroup reportGroup)
    {
        var body = new StringBuilder();

        body.AppendLine("<section class=\"mb-10\">");
        
        body.AppendLine($"<h2 class=\"text-2xl font-bold text-center\">{reportGroup.Title}</h2>");
        foreach (var report in reportGroup.Reports)
            body.Append(PrintInternal(report));

        body.AppendLine("</section>");

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

    private readonly List<TgChat> _dataSources;
    private readonly string _path;
}
