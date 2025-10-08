namespace TgJobAdAnalytics.Models.Reports.Html;

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
