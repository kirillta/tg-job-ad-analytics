using System.Globalization;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Enums;
using TgJobAdAnalytics.Models.Reports.Html;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class ChartBuilder
{
    internal static ChartModel Build(Report report)
    {
        var data = GetData(report);
        var isStacked = report.Type == ChartType.StackedBar;
        return new ChartModel(id: Guid.NewGuid(), type: report.Type.ToChartJsType(), data: data, isStacked: isStacked);
    }


    internal static ChartModel.DataModel BuildData(string label, Dictionary<string, double> results, double tension = 0.1)
    {
        var labels = results.Keys.ToList();
        var datasetData = results.Values.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList();
        var (bg, border) = GetPalette(datasetData.Count);
        var dataset = new ChartModel.DatasetModel(label: label, data: datasetData, backgroundColor: bg, borderColor: border, tension: tension);

        return new ChartModel.DataModel(labels: labels, dataset: dataset);
    }


    private static ChartModel.DataModel GetData(Report report)
    {
        var dataset = GetDataset(report);
        var labels = report.Results.Keys.ToList();

        List<ChartModel.DatasetModel>? additionalDatasets = null;
        if (report.SeriesOverlays is { Count: > 0 })
        {
            additionalDatasets = report.Type == ChartType.StackedBar
                ? BuildStackedBarOverlays(labels, report.SeriesOverlays)
                : BuildLineOverlays(labels, report.SeriesOverlays);
        }

        return new ChartModel.DataModel(labels, dataset, additionalDatasets);
    }


    private static List<ChartModel.DatasetModel> BuildStackedBarOverlays(List<string> labels, Dictionary<string, Dictionary<string, double>> overlays)
    {
        var datasets = new List<ChartModel.DatasetModel>();
        var colorIndex = 1;

        foreach (var (name, overlayResults) in overlays)
        {
            var data = labels
                .Select(l => overlayResults.TryGetValue(l, out var v) ? v.ToString(CultureInfo.InvariantCulture) : "0")
                .ToList();
            datasets.Add(new ChartModel.DatasetModel(
                label: name,
                data: data,
                backgroundColor: [_backgroundColors[colorIndex % _backgroundColors.Count]],
                borderColor: [_borderColors[colorIndex % _borderColors.Count]]));
            colorIndex++;
        }

        return datasets;
    }


    private static List<ChartModel.DatasetModel> BuildLineOverlays(List<string> labels, Dictionary<string, Dictionary<string, double>> overlays)
    {
        var datasets = new List<ChartModel.DatasetModel>();
        var colorIndex = 0;

        foreach (var (name, overlayResults) in overlays)
        {
            var data = labels
                .Select(l => overlayResults.TryGetValue(l, out var v) ? v.ToString(CultureInfo.InvariantCulture) : "0")
                .ToList();
            datasets.Add(new ChartModel.DatasetModel(
                label: name,
                data: data,
                backgroundColor: [_overlayBackgroundColors[colorIndex % _overlayBackgroundColors.Count]],
                borderColor: [_overlayBorderColors[colorIndex % _overlayBorderColors.Count]],
                tension: 0.4,
                typeOverride: "line",
                yAxisId: "y1"));
            colorIndex++;
        }

        return datasets;
    }


    private static ChartModel.DatasetModel GetDataset(Report report)
    {
        return report.Type switch
        {
            ChartType.Bar => GetBarDataset(report),
            ChartType.Doughnut => GetDoughnutDataset(report),
            ChartType.Line => GetLineDataset(report),
            ChartType.PolarArea => GetPolarAreaDataset(report),
            ChartType.StackedBar => GetStackedBarDataset(report),
            _ => throw new NotImplementedException()
        };
    }


    private static ChartModel.DatasetModel GetBarDataset(Report report)
        => new(label: report.Title, data: GetDatasetData(report), backgroundColor: GetPalette(report.Results.Count).bg, borderColor: GetPalette(report.Results.Count).border);


    private static ChartModel.DatasetModel GetDoughnutDataset(Report report)
        => new(label: report.Title, data: GetDatasetData(report), backgroundColor: GetPalette(report.Results.Count).bg, borderColor: GetPalette(report.Results.Count).border);


    private static ChartModel.DatasetModel GetLineDataset(Report report)
        => new(label: report.Title, data: GetDatasetData(report), backgroundColor: GetPalette(report.Results.Count).bg, borderColor: GetPalette(report.Results.Count).border, tension: 0.4);


    private static ChartModel.DatasetModel GetPolarAreaDataset(Report report)
        => new(label: report.Title, data: GetDatasetData(report), backgroundColor: GetPalette(report.Results.Count).bg, borderColor: GetPalette(report.Results.Count).border);


    private static ChartModel.DatasetModel GetStackedBarDataset(Report report)
        => new(label: report.Title, data: GetDatasetData(report), backgroundColor: [_backgroundColors[0]], borderColor: [_borderColors[0]]);


    private static List<string> GetDatasetData(Report report)
        => report.Results.Values
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToList();


    private static (List<string> bg, List<string> border) GetPalette(int count)
    {
        // Repeat base palette to match required length
        var bg = new List<string>(capacity: count);
        var border = new List<string>(capacity: count);
        for (var i = 0; i < count; i++)
        {
            var idx = i % _backgroundColors.Count;
            bg.Add(_backgroundColors[idx]);
            border.Add(_borderColors[idx]);
        }
        return (bg, border);
    }


    private readonly static List<string> _overlayBackgroundColors =
    [
        "rgba(255, 99, 132, 0.15)",
        "rgba(54, 162, 235, 0.15)",
        "rgba(255, 206, 86, 0.15)",
        "rgba(153, 102, 255, 0.15)",
        "rgba(255, 159, 64, 0.15)",
        "rgba(75, 192, 192, 0.15)"
    ];


    private readonly static List<string> _overlayBorderColors =
    [
        "rgba(255, 99, 132, 1)",
        "rgba(54, 162, 235, 1)",
        "rgba(255, 206, 86, 1)",
        "rgba(153, 102, 255, 1)",
        "rgba(255, 159, 64, 1)",
        "rgba(75, 192, 192, 1)"
    ];


    private readonly static List<string> _backgroundColors =
    [
        "rgba(75, 192, 192, 0.2)",
        "rgba(192, 75, 192, 0.2)",
        "rgba(192, 192, 75, 0.2)",
        "rgba(75, 75, 192, 0.2)",
        "rgba(192, 75, 75, 0.2)",
        "rgba(75, 192, 75, 0.2)",
        "rgba(75, 75, 75, 0.2)",
        "rgba(192, 192, 192, 0.2)",
        "rgba(128, 128, 128, 0.2)",
        "rgba(255, 99, 132, 0.2)",
        "rgba(54, 162, 235, 0.2)",
        "rgba(255, 206, 86, 0.2)"
    ];


    private readonly static List<string> _borderColors =
    [
        "rgba(75, 192, 192, 1)",
        "rgba(192, 75, 192, 1)",
        "rgba(192, 192, 75, 1)",
        "rgba(75, 75, 192, 1)",
        "rgba(192, 75, 75, 1)",
        "rgba(75, 192, 75, 1)",
        "rgba(75, 75, 75, 1)",
        "rgba(192, 192, 192, 1)",
        "rgba(128, 128, 128, 1)",
        "rgba(255, 99, 132, 1)",
        "rgba(54, 162, 235, 1)",
        "rgba(255, 206, 86, 1)"
    ];
}
