using TgJobAdAnalytics.Models.Reports.Enums;

namespace TgJobAdAnalytics.Models.Reports;

/// <summary>
/// Represents a single analytic report consisting of a primary result set and optional variant (comparative) datasets
/// together with rendering metadata (chart type and title).
/// </summary>
public readonly record struct Report
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Report"/> record struct.
    /// </summary>
    /// <param name="title">Human friendly report title.</param>
    /// <param name="results">Primary keyed numeric result set (e.g. category → value).</param>
    /// <param name="type">Preferred visualization style for the report.</param>
    /// <param name="variants">Optional nested variant result sets (e.g. scenario → (category → value)).</param>
    public Report(string title, Dictionary<string, double> results, ChartType type = ChartType.Bar, Dictionary<string, Dictionary<string, double>>? variants = null)
    {
        Results = results;
        Title = title;
        Type = type;
        Variants = variants;
    }


    /// <summary>
    /// Gets the primary result set mapping labels to numeric values.
    /// </summary>
    public Dictionary<string, double> Results { get; init; }

    /// <summary>
    /// Gets the report title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Gets the preferred chart visualization type.
    /// </summary>
    public ChartType Type { get; init; }

    /// <summary>
    /// Gets the optional variant datasets providing alternative or comparative result sets.
    /// </summary>
    public Dictionary<string, Dictionary<string, double>>? Variants { get; init; }
}
