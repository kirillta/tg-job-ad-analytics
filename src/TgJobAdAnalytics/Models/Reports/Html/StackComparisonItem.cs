namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents per-stack salary statistics for the last closed calendar month.
/// </summary>
public sealed class StackComparisonItem
{
    /// <summary>
    /// Gets or sets the stack unique identifier.
    /// </summary>
    public Guid StackId { get; set; }

    /// <summary>
    /// Gets or sets the canonical stack name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label for the stack. Defaults to the canonical name if not localized.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of salary samples for this stack in the period.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the 25th percentile of normalized salary values for this stack.
    /// </summary>
    public double P25 { get; set; }

    /// <summary>
    /// Gets or sets the median (50th percentile) of normalized salary values for this stack.
    /// </summary>
    public double Median { get; set; }

    /// <summary>
    /// Gets or sets the 75th percentile of normalized salary values for this stack.
    /// </summary>
    public double P75 { get; set; }
}
