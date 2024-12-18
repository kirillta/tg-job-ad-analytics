namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct DataSourceModel
{
    internal DataSourceModel(long id, string name, DateOnly minimalDate, DateOnly maximalDate)
    {
        Id = id;
        Name = name;
        MinimalDate = minimalDate;
        MaximalDate = maximalDate;
    }


    public long Id { get; }
    public string Name { get; }
    public DateOnly MinimalDate { get; }
    public DateOnly MaximalDate { get; }
}
