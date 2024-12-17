namespace TgJobAdAnalytics.Models.Reports.Html;

internal class ReportModel
{
    public string Body { get; set; }
    public List<DataSourceModel> DataSources { get; set; }
    public string ReportDate { get; set; }
    public List<ReportItem> Reports { get; set; }
}
