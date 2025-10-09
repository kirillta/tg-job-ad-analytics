namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents global salary statistics for all job ads.
/// </summary>
public sealed class GlobalSalaryStatistics
{

    /// <summary>
    /// Gets or sets the salary distribution buckets.
    /// </summary>
    public List<DistributionBucket> Distribution { get; set; } = new();


    /// <summary>
    /// Gets or sets the aggregate statistics summary.
    /// </summary>
    public AggregateStatistics Aggregates { get; set; } = new();
}
