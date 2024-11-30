namespace TgJobAdAnalytics.Services.Reports;

public static class ChartTypeExtensions
{
    public static string ToChartJsType(this ChartType chartType)
    {
        return chartType switch
        {
            ChartType.Bar => "bar",
            ChartType.Doughnut => "doughnut",
            ChartType.Line => "line",
            ChartType.PolarArea => "polarArea",
            ChartType.None => "undefined",
            _ => "undefined"
        };
    }
}
