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
    /// <param name="seriesOverlays">Optional always-visible additional series rendered as lines over the primary chart.</param>
    /// <param name="primarySeriesLabel">Optional localization key for the primary series label (used in stacked bar charts instead of the chart title).</param>
    public Report(string title, Dictionary<string, double> results, ChartType type = ChartType.Bar, Dictionary<string, Dictionary<string, double>>? variants = null, Dictionary<string, Dictionary<string, double>>? seriesOverlays = null, string? primarySeriesLabel = null)
    {
        Results = results;
        Title = title;
        Type = type;
        Variants = variants;
        SeriesOverlays = seriesOverlays;
        PrimarySeriesLabel = primarySeriesLabel;
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

    /// <summary>
    /// Gets the optional always-visible overlay series rendered as lines over the primary chart.
    /// </summary>
    public Dictionary<string, Dictionary<string, double>>? SeriesOverlays { get; init; }

    /// <summary>
    /// Gets the optional localization key for the primary series label in stacked bar charts.
    /// When set, the primary dataset label is resolved from this key instead of the chart title.
    /// </summary>
    public string? PrimarySeriesLabel { get; init; }
}
