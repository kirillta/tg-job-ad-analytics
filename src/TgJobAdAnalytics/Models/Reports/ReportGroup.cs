namespace TgJobAdAnalytics.Models.Reports;

/// <summary>
/// Represents a logical grouping of related <see cref="Report"/> instances under a shared title
/// to aid presentation, navigation, and aggregation in generated analytics output.
/// </summary>
public readonly record struct ReportGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGroup"/> record struct.
    /// </summary>
    /// <param name="title">The group title displayed above the contained reports.</param>
    /// <param name="reports">The collection of reports that belong to this group (order preserved).</param>
    public ReportGroup(string title, List<Report> reports)
    {
        Title = title;
        Reports = reports;
    }


    /// <summary>
    /// Gets the title describing this group of reports.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Gets the list of reports contained in the group.
    /// </summary>
    public List<Report> Reports { get; init; }
}
