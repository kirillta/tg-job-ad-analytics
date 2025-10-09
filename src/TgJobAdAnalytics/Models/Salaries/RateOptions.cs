namespace TgJobAdAnalytics.Models.Salaries;

/// <summary>
/// Configuration options for obtaining and loading currency exchange rates used in salary normalization.
/// </summary>
public sealed class RateOptions
{
    /// <summary>
    /// Gets or sets the base URI of the remote rate API endpoint (e.g. ECB / fixer service).
    /// </summary>
    public required Uri RateApiUrl { get; set; }

    /// <summary>
    /// Gets or sets an optional relative or absolute path to a local cached / embedded rate source (e.g. JSON file).
    /// Used as a fallback or for seeding initial data when the API cannot be reached.
    /// </summary>
    public string RateSourcePath { get; set; } = string.Empty;
}
