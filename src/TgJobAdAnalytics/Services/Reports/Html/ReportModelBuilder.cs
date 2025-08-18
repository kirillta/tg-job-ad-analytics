using TgJobAdAnalytics.Models.Reports.Html;

namespace TgJobAdAnalytics.Services.Reports.Html;

internal class ReportModelBuilder
{
    public static ReportModel Build(List<ReportItemGroup> groups, List<DataSourceModel> dataSources) 
        => new(groups, DateOnly.FromDateTime(DateTime.UtcNow), dataSources);
}
