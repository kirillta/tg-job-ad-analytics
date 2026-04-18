namespace TgJobAdAnalytics.Models.Reports.Enums;

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
            ChartType.StackedBar => "bar",
            ChartType.None => "undefined",
            _ => "undefined"
        };
    }
}
