namespace TgJobAdAnalytics.Models.Reports;

public readonly record struct ReportGroup
{
    public ReportGroup(string title, List<Report> reports)
    {
        Title = title;
        Reports = reports;
    }


    public string Title { get; init; }
    public List<Report> Reports { get; init; }
}
