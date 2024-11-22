namespace TgJobAdAnalytics.Models.Reports;

public static class ReportExtensions
{ 
    public static Report ToReport(this Dictionary<string, string> results, string title) 
        => new (title, results);
}
