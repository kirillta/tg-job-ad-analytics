namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportModel
{
    internal ReportModel(
        List<ReportItemGroup> reportGroups,
        DateOnly reportDate,
        List<DataSourceModel> dataSources,
        ReportPageMetadata metadata,
        Dictionary<string, object> localization,
        List<string> locales,
        string currentLocale,
        List<StackComparisonItem>? stackComparison = null)
    {
        DataSources = dataSources;
        ReportDate = reportDate;
        ReportGroups = reportGroups;
        Metadata = metadata;
        Localization = localization;
        Locales = locales;
        CurrentLocale = currentLocale;
        StackComparison = stackComparison ?? new List<StackComparisonItem>();
    }


    public List<DataSourceModel> DataSources { get; }

    public DateOnly ReportDate { get; }

    public List<ReportItemGroup> ReportGroups { get; }

    public ReportPageMetadata Metadata { get; }

    public Dictionary<string, object> Localization { get; }

    public List<string> Locales { get; }

    public string CurrentLocale { get; }

    public List<StackComparisonItem> StackComparison { get; }
}
