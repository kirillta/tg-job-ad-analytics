namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct DataSourceModel
{
    internal DataSourceModel(long id, string name, DateOnly minimalDate, DateOnly maximalDate, int processedMessages, int extractedSalaries, int lastMonthSalaries)
    {
        Id = id;
        Name = name;
        MinimalDate = minimalDate;
        MaximalDate = maximalDate;
        ProcessedMessages = processedMessages;
        ExtractedSalaries = extractedSalaries;
        LastMonthSalaries = lastMonthSalaries;
    }


    public long Id { get; }

    public string Name { get; }

    public DateOnly MinimalDate { get; }

    public DateOnly MaximalDate { get; }

    public int ProcessedMessages { get; }

    public int ExtractedSalaries { get; }

    public int LastMonthSalaries { get; }
}
