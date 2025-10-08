namespace TgJobAdAnalytics.Models.Reports.Html;

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
