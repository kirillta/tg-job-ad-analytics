namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents aggregate salary statistics.
/// </summary>
public sealed class AggregateStatistics
{
    /// <summary>
    /// Gets or sets the total number of job ads.
    /// </summary>
    public int TotalJobs { get; set; }


    /// <summary>
    /// Gets or sets the median salary value.
    /// </summary>
    public double MedianSalary { get; set; }


    /// <summary>
    /// Gets or sets the mean salary value.
    /// </summary>
    public double MeanSalary { get; set; }


    /// <summary>
    /// Gets or sets the percentile values (p10, p25, p50, p75, p90).
    /// </summary>
    public Dictionary<string, double> Percentiles { get; set; } = new();
}
