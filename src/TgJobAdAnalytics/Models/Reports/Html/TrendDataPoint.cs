namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Represents a single data point in a salary trend time series.
/// </summary>
public sealed class TrendDataPoint
{
    /// <summary>
    /// Gets or sets the date in YYYY-MM format.
    /// </summary>
    public string Date { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the median salary for this period.
    /// </summary>
    public double Median { get; set; }


    /// <summary>
    /// Gets or sets the mean salary for this period.
    /// </summary>
    public double Mean { get; set; }


    /// <summary>
    /// Gets or sets the number of job ads in this period.
    /// </summary>
    public int Count { get; set; }


    /// <summary>
    /// Gets or sets the 25th percentile salary.
    /// </summary>
    public double P25 { get; set; }


    /// <summary>
    /// Gets or sets the 75th percentile salary.
    /// </summary>
    public double P75 { get; set; }
}
