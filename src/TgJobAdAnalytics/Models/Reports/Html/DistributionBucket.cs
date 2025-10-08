namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents a salary distribution bucket.
/// </summary>
public sealed class DistributionBucket
{
    /// <summary>
    /// Gets or sets the bucket label (e.g., "50k-100k").
    /// </summary>
    public string Bucket { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the count of job ads in this bucket.
    /// </summary>
    public int Count { get; set; }


    /// <summary>
    /// Gets or sets the percentage of total ads in this bucket.
    /// </summary>
    public double Percentage { get; set; }
}
