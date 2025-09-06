namespace TgJobAdAnalytics.Models.Reports;

public readonly record struct Report
{
    public Report(string title, Dictionary<string, double> results, ChartType type = ChartType.Bar, Dictionary<string, Dictionary<string, double>>? variants = null)
    {
        Results = results;
        Title = title;
        Type = type;
        Variants = variants;
    }

    
    public Dictionary<string, double> Results { get; init; }
    public string Title { get; init; }
    public ChartType Type { get; init; }
    public Dictionary<string, Dictionary<string, double>>? Variants { get; init; }
}
