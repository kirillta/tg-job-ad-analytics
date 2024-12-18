namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportItem
{
    internal ReportItem(string title, List<KeyValuePair<string, string>> results)
    {
        Title = title;
        Results = results;
    }


    public string Title { get; }
    public List<KeyValuePair<string, string>> Results { get; }
}
