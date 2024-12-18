using System.Text;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports.Console;

public sealed class ConsoleReportPrinter : IReportPrinter
{
    public void Print(IEnumerable<ReportGroup> reportGroups)
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        foreach (var reportGroup in reportGroups)
        {
            System.Console.WriteLine($"** {reportGroup.Title} **");
            System.Console.WriteLine();
            Print(reportGroup.Reports);
        }
    }


    public void Print(IEnumerable<Report> reports)
    {
        foreach (var report in reports)
            Print(report);
    }


    public void Print(Report report)
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
