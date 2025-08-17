namespace TgJobAdAnalytics.Models.Salaries;

public readonly record struct SalaryExtractionResult
{
    public required Guid SalaryEntityId { get; init; }
}