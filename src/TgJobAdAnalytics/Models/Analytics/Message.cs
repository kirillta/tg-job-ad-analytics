
using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct Message
{
    [JsonConstructor]
    public Message(long id, DateOnly date, string text, ChatInfo chatInfo, long chatId, Salary salary)
    {
        Id = id;
        Chat = chatInfo;
        ChatId = chatId;
        Date = date;
        Text = text;
        Salary = salary;
    }


    public long Id { get; init; }
    public ChatInfo Chat { get; init; }
    public long ChatId { get; init; }
    public DateOnly Date { get; init; }
    public Salary Salary { get; init; }
    public string Text { get; init; }
}
