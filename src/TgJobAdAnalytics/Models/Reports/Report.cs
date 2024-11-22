namespace TgJobAdAnalytics.Models.Reports;

public readonly record struct Report
{
    public Report(string title, Dictionary<string, string> results)
    {
        Title = title;
        Results = results;
    }


    public string Title { get; init; }
    public Dictionary<string, string> Results { get; init; }
}
