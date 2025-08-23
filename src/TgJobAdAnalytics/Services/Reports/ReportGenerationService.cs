using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Services.Analytics;

namespace TgJobAdAnalytics.Services.Reports;

public class ReportGenerationService
{
    public ReportGenerationService(ApplicationDbContext dbContext)
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
            AdStatsCalculator.GenerateAll(salaries),
            SalaryStatisticsCalculator.GenerateAll(salaries)
        ];
    }


    private readonly ApplicationDbContext _dbContext;
}
