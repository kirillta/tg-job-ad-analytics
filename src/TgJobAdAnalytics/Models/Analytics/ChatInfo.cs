namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct ChatInfo
{
    public ChatInfo(long id, string title)
    {
        Id = id;
        Title = title;
    }


    public long Id { get; init; }
    public string Title { get; init; }
}
