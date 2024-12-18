namespace TgJobAdAnalytics.Models.Reports;

public static class ReportExtensions
{ 
    public static Report ToReport(this Dictionary<string, double> results, string title, ChartType type = ChartType.Bar) 
        => new (title, results, type);
}
