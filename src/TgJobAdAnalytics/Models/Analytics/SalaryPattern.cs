using System.Text.RegularExpressions;

namespace TgJobAdAnalytics.Models.Analytics;

public readonly record struct SalaryPattern
{
    public SalaryPattern(Regex regex, Currency currency, string description)
    {
        Regex = regex;
        Currency = currency;
        Description = description;
    }


    public Regex Regex { get; init; }
    public Currency Currency { get; init; }
    public string Description { get; init; }
}
