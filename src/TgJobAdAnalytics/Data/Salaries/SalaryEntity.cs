using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Data.Salaries;

public class SalaryEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AdId { get; init; }
    public DateOnly Date { get; init; }
    public Currency? Currency { get; init; }
    public Currency CurrencyNormalized { get; set; }
    public double? LowerBound { get; set; }
    public double LowerBoundNormalized { get; set; }
    public double? UpperBound { get; set; }
    public double UpperBoundNormalized { get; set; }
    public Period? Period { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.NotStarted;
    public PositionLevel Level { get; set; } = PositionLevel.Unknown;
}

