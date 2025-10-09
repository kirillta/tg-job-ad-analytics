namespace TgJobAdAnalytics.Models.Reports;

/// <summary>
/// Options controlling report generation and output, including target output directory and HTML (or other) template location.
/// </summary>
public sealed class ReportPrinterOptions
{
    /// <summary>
    /// Gets or sets the directory path where generated report files will be written.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the template file used when rendering reports.
    /// </summary>
    public string TemplatePath { get; set; } = string.Empty;
}
