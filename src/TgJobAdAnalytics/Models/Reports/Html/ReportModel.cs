namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportModel
{
    internal ReportModel(List<ReportItemGroup> reportGroups, DateOnly reportDate, List<DataSourceModel> dataSources)
    {
        DataSources = dataSources;
        ReportDate = reportDate;
        ReportGroups = reportGroups;
    }


    public List<DataSourceModel> DataSources { get; }

    public DateOnly ReportDate { get; }

    public List<ReportItemGroup> ReportGroups { get; }
}
