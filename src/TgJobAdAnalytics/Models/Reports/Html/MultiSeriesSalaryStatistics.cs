namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents multi-series salary statistics with global and per-stack breakdowns.
/// </summary>
public sealed class MultiSeriesSalaryStatistics
{
    /// <summary>
    /// Gets or sets the global statistics for all job ads combined.
    /// </summary>
    public GlobalSalaryStatistics Global { get; set; } = new();


    /// <summary>
    /// Gets or sets the per-stack statistics dictionary keyed by normalized stack name.
    /// </summary>
    public Dictionary<string, StackSalaryStatistics> ByStack { get; set; } = new();


    /// <summary>
    /// Gets or sets the yearly salary statistics with position level breakdowns.
    /// </summary>
    public YearlySalaryStatistics YearlyStats { get; set; } = new();


    /// <summary>
    /// Gets or sets the metadata about the statistics.
    /// </summary>
    public StatisticsMetadata Metadata { get; set; } = new();


    /// <summary>
    /// Gets or sets the list of available stacks with summary information.
    /// </summary>
    public List<StackSummary> Stacks { get; set; } = new();

    /// <summary>
    /// Gets or sets per-stack yearly statistics (stack name -> yearly stats).
    /// </summary>
    public Dictionary<string, YearlySalaryStatistics> YearlyByStack { get; set; } = new();
}
