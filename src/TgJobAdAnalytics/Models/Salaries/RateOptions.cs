namespace TgJobAdAnalytics.Models.Salaries;

public sealed class RateOptions
{
    public required Uri RateApiUrl { get; set; }
    public string RateSourcePath { get; set; } = string.Empty;
}
