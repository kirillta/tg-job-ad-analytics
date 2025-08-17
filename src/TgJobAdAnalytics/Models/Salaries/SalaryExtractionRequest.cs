using TgJobAdAnalytics.Data.Messages;

namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct SalaryExtractionRequest
{
    public required AdEntity Ad { get; init; }
}
