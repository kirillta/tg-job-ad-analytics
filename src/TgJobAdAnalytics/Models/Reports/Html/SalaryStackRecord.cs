using TgJobAdAnalytics.Data.Salaries;

namespace TgJobAdAnalytics.Models.Reports.Html;

/// <summary>
/// A salary entry paired with its associated technology stack, used to consolidate report data loading into a single query.
/// </summary>
/// <param name="Salary">The salary entity.</param>
/// <param name="StackId">The associated technology stack identifier, or <c>null</c> when the originating ad has no stack assigned.</param>
/// <param name="StackName">The name of the technology stack, or <c>null</c> when the originating ad has no stack assigned.</param>
public sealed record SalaryStackRecord(SalaryEntity Salary, Guid? StackId, string? StackName);
