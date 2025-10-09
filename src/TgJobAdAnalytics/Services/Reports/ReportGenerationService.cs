using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Services.Analytics;

namespace TgJobAdAnalytics.Services.Reports;

/// <summary>
/// Generates analytical report groups by aggregating persisted domain data (e.g. salaries, ads)
/// into higher-level metrics and statistics. Each invocation produces a fresh snapshot.
/// </summary>
public class ReportGenerationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerationService"/>.
    /// </summary>
    /// <param name="dbContext">Application database context supplying source data for reports.</param>
    public ReportGenerationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    /// <summary>
    /// Builds all configured report groups (ad statistics, salary statistics, etc.).
    /// Salaries from the current month (UTC) are excluded to avoid skew from partial month data.
    /// </summary>
    /// <returns>List of populated <see cref="ReportGroup"/> instances.</returns>
    public List<ReportGroup> Generate()
    {
        var firstDayOfCurrentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var salaries = _dbContext.Salaries
            .Where(s => s.Date < firstDayOfCurrentMonth)
            .ToList();

        return
        [
            AdStatsCalculator.GenerateAll(salaries),
            SalaryStatisticsCalculator.GenerateAll(salaries)
        ];
    }


    private readonly ApplicationDbContext _dbContext;
}
