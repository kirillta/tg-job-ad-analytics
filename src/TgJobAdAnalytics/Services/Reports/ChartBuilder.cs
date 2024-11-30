using System.Globalization;
using System.Text;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Reports;

public sealed class ChartBuilder
{
    public static StringBuilder Build(Report report)
    {
        var chart = new StringBuilder();

        var chartId = $"chart-{Guid.NewGuid()}";
        chart.AppendLine($"<div style=\"max-width: 100vw; max-height: 75vh;\">");
        chart.AppendLine($"<canvas id=\"{chartId}\"></canvas>");
        chart.AppendLine("</div>");

        chart.AppendLine("<script>");
        chart.AppendLine("var ctx = document.getElementById('" + chartId + "').getContext('2d');");
        chart.AppendLine("var chart = new Chart(ctx, ");
        chart.Append(GetConfig(report));
        chart.AppendLine(");");
        chart.AppendLine("</script>");

        return chart;
    }


    private static StringBuilder GetConfig(Report report)
    {
        var config = new StringBuilder();
        config.AppendLine("{");

        config.AppendLine($"type: '{report.Type.ToChartJsType()}',");

        config.AppendLine("options: ");
        config.Append(GetOptions() + ',');

        config.AppendLine("data: ");
        config.Append(GetData(report));

        config.AppendLine("}");

        return config;
    }


    private static StringBuilder GetData(Report report)
    {
        var data = new StringBuilder();

        var dataset = GetDataset(report);

        data.AppendLine("{");
        data.AppendLine("labels: [" + string.Join(",", report.Results.Keys.Select(k => $"'{k}'")) + "],");

        data.AppendLine("datasets: [");
        data.Append(dataset);
        data.AppendLine("]");

        data.AppendLine("}");

        return data;


        StringBuilder GetBarDataset(Report report)
        {
            var dataset = new StringBuilder();

            dataset.AppendLine("{");
            dataset.AppendLine("label: '" + report.Title + "',");
            dataset.AppendLine("data: [" + GetDatasetData(report) + "],");
            dataset.AppendLine($"backgroundColor: '{_backgroundColors.First()}',");
            dataset.AppendLine($"borderColor: '{_borderColors.First()}',");
            dataset.AppendLine("borderWidth: 1");
            dataset.AppendLine("}");
            
            return dataset;
        }


        StringBuilder GetDoughnutDataset(Report report) 
        {
            var dataset = new StringBuilder();

            dataset.AppendLine("{");
            dataset.AppendLine("data: [" + GetDatasetData(report) + "],");
            
            dataset.AppendLine("backgroundColor: [");
            foreach (var color in _backgroundColors)
                dataset.AppendLine($"'{color}',");
            dataset.AppendLine("],");

            dataset.AppendLine("borderColor: [");
            foreach (var color in _borderColors)
                dataset.AppendLine($"'{color}',");
            dataset.AppendLine("],");

            dataset.AppendLine("borderWidth: 1");

            dataset.AppendLine("}");

            return dataset;
        }


        StringBuilder GetLineDataset(Report report) 
        {
            var dataset = new StringBuilder();

            dataset.AppendLine("{");
            dataset.AppendLine("label: '" + report.Title + "',");
            dataset.AppendLine("data: [" + GetDatasetData(report) + "],");
            dataset.AppendLine("fill: false,");
            dataset.AppendLine($"borderColor: '{_borderColors.First()}',");
            dataset.AppendLine("tension: 0.1");
            dataset.AppendLine("}");

            return dataset;
        }


        StringBuilder GetPolarAreaDataset(Report report)
        {
            var dataset = new StringBuilder();

            dataset.AppendLine("{");
            dataset.AppendLine("label: '" + report.Title + "',");
            dataset.AppendLine("data: [" + GetDatasetData(report) + "],");

            dataset.AppendLine("backgroundColor: [");
            foreach (var color in _backgroundColors)
                dataset.AppendLine($"'{color}',");
            dataset.AppendLine("],");

            dataset.AppendLine("borderColor: [");
            foreach (var color in _borderColors)
                dataset.AppendLine($"'{color}',");
            dataset.AppendLine("],");

            dataset.AppendLine("borderWidth: 1");

            dataset.AppendLine("}");
            
            return dataset;
        }


        StringBuilder GetDataset(Report report)
        {
            return report.Type switch
            {
                ChartType.Bar => GetBarDataset(report),
                ChartType.Doughnut => GetDoughnutDataset(report),
                ChartType.Line => GetLineDataset(report),
                ChartType.PolarArea => GetPolarAreaDataset(report),
                _ => throw new NotImplementedException()
            };
        }


        string GetDatasetData(Report report) 
            => string.Join(",", report.Results.Values.Select(x => x.ToString(CultureInfo.InvariantCulture)));
    }


    private static string GetOptions()
    {
        return @"
            {
                layout: {
                    padding: 50
                },
                maintainAspectRatio: true,
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    },
                },
            }
        ";
    }


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
