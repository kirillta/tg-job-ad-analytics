namespace TgJobAdAnalytics.Models.Reports.Html;

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
