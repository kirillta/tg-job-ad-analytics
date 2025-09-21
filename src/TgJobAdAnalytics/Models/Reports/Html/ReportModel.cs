namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportModel
{
    internal ReportModel(List<ReportItemGroup> reportGroups, DateOnly reportDate, List<DataSourceModel> dataSources, ReportPageMetadata metadata)
    {
        DataSources = dataSources;
        ReportDate = reportDate;
        ReportGroups = reportGroups;
        Metadata = metadata;
    }


    public List<DataSourceModel> DataSources { get; }

    public DateOnly ReportDate { get; }

    public List<ReportItemGroup> ReportGroups { get; }

    public ReportPageMetadata Metadata { get; }
}
