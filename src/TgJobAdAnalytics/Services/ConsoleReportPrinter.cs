using System.Text;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services;

public static class ConsoleReportPrinter
{
    public static void Print(IEnumerable<ReportGroup> reportGroups)
    {
        Console.OutputEncoding = Encoding.UTF8;

        foreach (var reportGroup in reportGroups)
        {
            Console.WriteLine($"** {reportGroup.Title} **");
            Console.WriteLine();
            Print(reportGroup.Reports);
        }
    }


    public static void Print(IEnumerable<Report> reports)
    {
        foreach (var report in reports)
            Print(report);
    }


    public static void Print(Report report)
    {
        const int padding = 20;

        Console.WriteLine(report.Title);
        Console.WriteLine(new string('-', padding * 2));
        foreach (var result in report.Results)
        {
            var key = result.Key.PadRight(padding);
            var value = result.Value.PadRight(padding);
            Console.WriteLine($"{key}: {value}");
        }

        Console.WriteLine();
        Console.WriteLine();
    }
}
