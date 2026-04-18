using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Locations.Enums;

namespace TgJobAdAnalytics.Models.Salaries;

/// <summary>
/// Holds the per-ad extraction result: optional salary data plus always-present location and work format classifications.
/// </summary>
public readonly record struct SalaryExtractionResult
{
    /// <summary>
    /// Initializes a new <see cref="SalaryExtractionResult"/>.
    /// </summary>
    public SalaryExtractionResult(SalaryEntity? salary, VacancyLocation location, WorkFormat format)
    {
        Salary = salary;
        Location = location;
        Format = format;
    }


    /// <summary>
    /// Extracted salary entity, or null if no salary was detected.
    /// </summary>
    public SalaryEntity? Salary { get; init; }

    /// <summary>
    /// Detected vacancy location.
    /// </summary>
    public VacancyLocation Location { get; init; }

    /// <summary>
    /// Detected work format.
    /// </summary>
    public WorkFormat Format { get; init; }
}
