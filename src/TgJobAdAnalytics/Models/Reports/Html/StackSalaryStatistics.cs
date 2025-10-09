namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents salary statistics for a specific technology stack.
/// </summary>
public sealed class StackSalaryStatistics
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
