using System.Text;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports.Console;

public sealed class ConsoleReportExporter : IReportExporter
{
    public void Write(IEnumerable<ReportGroup> reportGroups)
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        foreach (var reportGroup in reportGroups)
        {
            System.Console.WriteLine($"** {reportGroup.Title} **");
            System.Console.WriteLine();
            Write(reportGroup.Reports);
        }
    }


    public void Write(IEnumerable<Report> reports)
    {
        foreach (var report in reports)
            Write(report);
    }


    public void Write(Report report)
    {
        const int padding = 20;

        System.Console.WriteLine(report.Title);
        System.Console.WriteLine(new string('-', padding * 2));
        foreach (var result in report.Results)
        {
            var key = result.Key.PadRight(padding);
            var value = result.Value.ToString().PadRight(padding);
            System.Console.WriteLine($"{key}: {value}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine();
    }
}
