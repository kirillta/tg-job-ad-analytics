using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports;

/// <summary>
/// Exports analytic report data (individual reports or grouped collections) to a target medium
/// (e.g. file system, console, HTML output). Implementations decide formatting and destination.
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// Writes a sequence of report groups (each containing one or more reports) to the export target.
    /// </summary>
    /// <param name="reportGroups">Grouped report definitions to export.</param>
    void Write(IEnumerable<ReportGroup> reportGroups);

    /// <summary>
    /// Writes a sequence of individual reports (no grouping metadata) to the export target.
    /// </summary>
    /// <param name="reports">Report definitions to export.</param>
    void Write(IEnumerable<Report> reports);

    /// <summary>
    /// Writes a single report to the export target.
    /// </summary>
    /// <param name="report">Report definition to export.</param>
    void Write(Report report);
}
