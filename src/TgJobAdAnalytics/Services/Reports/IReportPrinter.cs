using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports;

public interface IReportPrinter
{
    void Print(IEnumerable<ReportGroup> reportGroups);
    void Print(IEnumerable<Report> reports);
    void Print(Report report);
}
