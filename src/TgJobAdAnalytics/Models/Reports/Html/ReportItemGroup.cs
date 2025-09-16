namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportItemGroup
{
    internal ReportItemGroup(string title, List<ReportItem> reports)
    {
        Title = title;
        Reports = reports;
    }


    public string Title { get; }

    public List<ReportItem> Reports { get; }
}