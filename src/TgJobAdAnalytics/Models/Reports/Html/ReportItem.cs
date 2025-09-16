namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportItem
{
    internal ReportItem(string title, List<KeyValuePair<string, string>> results, ChartModel? chart, Dictionary<string, ChartModel.DataModel>? variants = null)
    {
        Chart = chart;
        Results = results;
        Title = title;
        Variants = variants;
    }


    public string Title { get; }

    public List<KeyValuePair<string, string>> Results { get; }

    public ChartModel? Chart { get; }

    public Dictionary<string, ChartModel.DataModel>? Variants { get; }
}
