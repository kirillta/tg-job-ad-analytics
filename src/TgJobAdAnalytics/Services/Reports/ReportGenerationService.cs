using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Locations.Enums;
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

        var ads = _dbContext.Ads.ToList();
        
        var stackNames = _dbContext.TechnologyStacks.ToDictionary(ts => ts.Id, ts => ts.Name);
        var adStackMapping = ads
            .Where(a => a.StackId != null && stackNames.ContainsKey(a.StackId.Value))
            .ToDictionary(a => a.Id, a => stackNames[a.StackId!.Value]);

        var adLocationMapping = ads
            .Where(a => a.Location != VacancyLocation.Unknown)
            .ToDictionary(a => a.Id, a => a.Location);

        var adWorkFormatMapping = ads
            .Where(a => a.WorkFormat != WorkFormat.Unknown)
            .ToDictionary(a => a.Id, a => a.WorkFormat);

        return
        [
            AdStatsCalculator.GenerateAll(salaries, adStackMapping, adLocationMapping, adWorkFormatMapping),
            SalaryStatisticsCalculator.GenerateAll(salaries)
        ];
    }


    private readonly ApplicationDbContext _dbContext;
}
