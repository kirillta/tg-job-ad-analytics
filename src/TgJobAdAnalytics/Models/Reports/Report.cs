namespace TgJobAdAnalytics.Models.Reports;

public readonly record struct Report
{
    public Report(string title, Dictionary<string, double> results, ChartType type = ChartType.Bar)
    {
        Results = results;
        Title = title;
        Type = type;
    }

    
    public Dictionary<string, double> Results { get; init; }
    public string Title { get; init; }
    public ChartType Type { get; init; }
}
