namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportItem
{
    internal ReportItem(string code, string title, List<KeyValuePair<string, string>> results, ChartModel? chart, Dictionary<string, ChartModel.DataModel>? variants = null)
    {
        Code = code;
        Title = title;
        Results = results;
        Chart = chart;
        Variants = variants;
    }

    public string Code { get; }

    public string Title { get; }

    public List<KeyValuePair<string, string>> Results { get; }

    public ChartModel? Chart { get; }

    public Dictionary<string, ChartModel.DataModel>? Variants { get; }
}
