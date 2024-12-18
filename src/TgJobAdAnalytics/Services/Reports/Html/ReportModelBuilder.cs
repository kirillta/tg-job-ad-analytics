using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Reports.Html;

internal class ReportModelBuilder
{
    public static ReportModel Build(List<ReportItemGroup> groups, List<TgChat> dataSources)
    {
        return new(groups, DateOnly.FromDateTime(DateTime.UtcNow), BuildDataSourceModels(dataSources));
    }


    private static List<DataSourceModel> BuildDataSourceModels(List<TgChat> dataSources)
        => dataSources.Select(source
            => new DataSourceModel
            (
                source.Id,
                source.Name,
                DateOnly.FromDateTime(source.Messages.Min(m => m.Date)),
                DateOnly.FromDateTime(source.Messages.Max(m => m.Date))
            )).ToList();
}
