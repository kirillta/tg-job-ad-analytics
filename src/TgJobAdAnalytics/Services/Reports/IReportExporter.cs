using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports;

public interface IReportExporter
{
    void Write(IEnumerable<ReportGroup> reportGroups);
    void Write(IEnumerable<Report> reports);
    void Write(Report report);
}
