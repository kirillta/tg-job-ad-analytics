
using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct Message
{
    [JsonConstructor]
    public Message(long id, DateOnly date, string text, Dictionary<string, int> termFrequency, Salary salary)
    {
        Id = id;
        Date = date;
        Text = text;
        TermFrequency = termFrequency;
        Salary = salary;
    }


    public long Id { get; init; }
    public DateOnly Date { get; init; }
    public Salary Salary { get; init; }
    public Dictionary<string, int> TermFrequency { get; init; }
    public string Text { get; init; }
}
