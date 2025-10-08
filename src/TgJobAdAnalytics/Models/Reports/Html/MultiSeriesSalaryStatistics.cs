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

/// <summary>
/// Represents global salary statistics for all job ads.
/// </summary>
public sealed class GlobalSalaryStatistics
{
    /// <summary>
    /// Gets or sets the salary trend data points over time.
    /// </summary>
    public List<TrendDataPoint> Trends { get; set; } = new();


    /// <summary>
    /// Gets or sets the salary distribution buckets.
    /// </summary>
    public List<DistributionBucket> Distribution { get; set; } = new();


    /// <summary>
    /// Gets or sets the aggregate statistics summary.
    /// </summary>
    public AggregateStatistics Aggregates { get; set; } = new();
}

/// <summary>
/// Represents salary statistics for a specific technology stack.
/// </summary>
public sealed class StackSalaryStatistics
{
    /// <summary>
    /// Gets or sets the salary trend data points over time.
    /// </summary>
    public List<TrendDataPoint> Trends { get; set; } = new();


    /// <summary>
    /// Gets or sets the salary distribution buckets.
    /// </summary>
    public List<DistributionBucket> Distribution { get; set; } = new();


    /// <summary>
    /// Gets or sets the aggregate statistics summary.
    /// </summary>
    public AggregateStatistics Aggregates { get; set; } = new();
}

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

/// <summary>
/// Represents yearly salary statistics with position level breakdowns.
/// </summary>
public sealed class YearlySalaryStatistics
{
    /// <summary>
    /// Gets or sets the minimum salary by year.
    /// </summary>
    public Dictionary<string, double> MinimumByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the maximum salary by year.
    /// </summary>
    public Dictionary<string, double> MaximumByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the average (mean) salary by year.
    /// </summary>
    public Dictionary<string, double> AverageByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the median salary by year.
    /// </summary>
    public Dictionary<string, double> MedianByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the per-level yearly statistics (level name -> statistics).
    /// </summary>
    public Dictionary<string, LevelYearlyStatistics> ByLevel { get; set; } = new();
}

/// <summary>
/// Represents yearly salary statistics for a specific position level.
/// </summary>
public sealed class LevelYearlyStatistics
{
    /// <summary>
    /// Gets or sets the minimum salary by year for this level.
    /// </summary>
    public Dictionary<string, double> MinimumByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the maximum salary by year for this level.
    /// </summary>
    public Dictionary<string, double> MaximumByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the average (mean) salary by year for this level.
    /// </summary>
    public Dictionary<string, double> AverageByYear { get; set; } = new();


    /// <summary>
    /// Gets or sets the median salary by year for this level.
    /// </summary>
    public Dictionary<string, double> MedianByYear { get; set; } = new();
}

/// <summary>
/// Represents summary information about a technology stack.
/// </summary>
public sealed class StackSummary
{
    /// <summary>
    /// Gets or sets the normalized stack identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the display name of the stack.
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the number of job ads for this stack.
    /// </summary>
    public int JobCount { get; set; }


    /// <summary>
    /// Gets or sets the percentage of total job ads using this stack.
    /// </summary>
    public double Percentage { get; set; }
}

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
