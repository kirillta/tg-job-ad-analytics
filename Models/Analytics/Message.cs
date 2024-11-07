
using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct Message
{
    [JsonConstructor]
    public Message(long id, DateTime date, string text, Salary salary)
    {
        Id = id;
        Date = date;
        Text = text;
        Salary = salary;
    }


    public long Id { get; init; }
    public DateTime Date { get; init; }
    public string Text { get; init; }
    public Salary Salary { get; init; }
}
