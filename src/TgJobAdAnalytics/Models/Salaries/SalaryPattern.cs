using System.Text.RegularExpressions;

namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct SalaryPattern
{
    public SalaryPattern(Regex regex, Currency currency, BoundaryType boundaryType, string description)
    {
        Regex = regex;
        BoundaryType = boundaryType;
        Currency = currency;
        Description = description;
    }


    public Regex Regex { get; init; }
    public BoundaryType BoundaryType { get; init; }
    public Currency Currency { get; init; }
    public string Description { get; init; }
}
