using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Analytics;

public class AnalyticsService
{
    public AnalyticsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public List<ReportGroup> Generate()
    {
        var firstDayOfCurrentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
        var salaries = _dbContext.Salaries
            .Where(s => s.Date < firstDayOfCurrentMonth)
            .ToList();

        return
        [
            AdStatsCalculator.CalculateAll(salaries),
            SalaryCalculator.CalculateAll(salaries)
        ];
    }


    private readonly ApplicationDbContext _dbContext;
}
