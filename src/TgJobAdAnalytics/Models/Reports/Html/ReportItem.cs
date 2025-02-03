namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportItem
{
    internal ReportItem(string title, List<KeyValuePair<string, string>> results, ChartModel? chart)
    {
        Chart = chart;
        Results = results;
        Title = title;
    }


    public string Title { get; }
    public List<KeyValuePair<string, string>> Results { get; }
    public ChartModel? Chart { get; }
}
