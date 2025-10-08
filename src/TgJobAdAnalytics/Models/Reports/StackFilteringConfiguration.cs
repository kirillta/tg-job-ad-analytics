namespace TgJobAdAnalytics.Models.Reports;

/// <summary>
/// Configuration for client-side stack filtering feature in HTML reports.
/// </summary>
public sealed class StackFilteringConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of stacks to include in per-stack statistics.
    /// </summary>
    public int TopStacksLimit { get; set; } = 30;


    /// <summary>
    /// Gets or sets the minimum number of jobs required for a stack to be included in statistics.
    /// </summary>
    public int MinimumJobCount { get; set; } = 5;


    /// <summary>
    /// Gets or sets the default filter mode. Valid values: "all", "include", "exclude".
    /// </summary>
    public string DefaultMode { get; set; } = "all";
}
