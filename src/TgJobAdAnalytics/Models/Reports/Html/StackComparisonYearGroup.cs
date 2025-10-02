namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents a group of per-stack salary stats for a specific year.
/// </summary>
public sealed class StackComparisonYearGroup
{
    /// <summary>
    /// Gets or sets the calendar year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the per-stack items for this year.
    /// </summary>
    public List<StackComparisonItem> Items { get; set; } = new();
}
