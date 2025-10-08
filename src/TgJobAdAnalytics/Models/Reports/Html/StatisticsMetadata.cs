namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents metadata about the statistics.
/// </summary>
public sealed class StatisticsMetadata
{
    /// <summary>
    /// Gets or sets the timestamp when statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }


    /// <summary>
    /// Gets or sets the start date of the date range.
    /// </summary>
    public DateTime DateRangeFrom { get; set; }


    /// <summary>
    /// Gets or sets the end date of the date range.
    /// </summary>
    public DateTime DateRangeTo { get; set; }


    /// <summary>
    /// Gets or sets the total number of job ads in the dataset.
    /// </summary>
    public int TotalJobs { get; set; }


    /// <summary>
    /// Gets or sets the number of unique stacks in the dataset.
    /// </summary>
    public int StackCount { get; set; }
}
