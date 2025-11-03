namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// Per-level statistics for a stack within a period (count and percentiles).
/// </summary>
public sealed class StackLevelStats
{
 /// <summary>
 /// Gets or sets the number of salary samples for this level.
 /// </summary>
 public int Count { get; set; }

 /// <summary>
 /// Gets or sets the10th percentile of normalized salary values for this level.
 /// </summary>
 public double P10 { get; set; }

 /// <summary>
 /// Gets or sets the25th percentile of normalized salary values for this level.
 /// </summary>
 public double P25 { get; set; }

 /// <summary>
 /// Gets or sets the median (50th percentile) for this level.
 /// </summary>
 public double Median { get; set; }

 /// <summary>
 /// Gets or sets the75th percentile of normalized salary values for this level.
 /// </summary>
 public double P75 { get; set; }

 /// <summary>
 /// Gets or sets the90th percentile of normalized salary values for this level.
 /// </summary>
 public double P90 { get; set; }
}
