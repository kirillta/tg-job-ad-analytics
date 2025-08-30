using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Levels;

/// <summary>
/// Backfills or refreshes position levels for existing salary entries that were persisted
/// before level detection was introduced.
/// </summary>
public sealed class SalaryLevelUpdateProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryLevelUpdateProcessor"/> class.
    /// </summary>
    public SalaryLevelUpdateProcessor(ILoggerFactory loggerFactory, ApplicationDbContext dbContext)
    {
        _logger = loggerFactory.CreateLogger<SalaryLevelUpdateProcessor>();
        _dbContext = dbContext;
    }


    /// <summary>
    /// Updates salary level for all salaries where the level is <see cref="PositionLevel.Unknown"/>.
    /// </summary>
    /// <returns>The number of salary records updated.</returns>
    public async Task<int> UpdateMissingLevels()
    {
        var items = await _dbContext.Salaries
            .Where(s => s.Level == PositionLevel.Unknown)
            .Join(_dbContext.Ads, s => s.AdId, a => a.Id, (s, a) => new { Salary = s, a.MessageId })
            .Join(_dbContext.Messages, sa => sa.MessageId, m => m.Id, (sa, m) => new { sa.Salary, m.Tags })
            .AsTracking()
            .ToListAsync();

        if (items.Count == 0)
        {
            _logger.LogInformation("No salaries with unknown level found");
            return 0;
        }

        var updated = 0;
        foreach (var item in items)
        {
            var level = PositionLevelResolver.Resolve(item.Tags);
            if (level == PositionLevel.Unknown)
                continue;

            if (item.Salary.Level != level)
            {
                item.Salary.Level = level;
                updated++;
            }
        }

        if (updated > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Updated {Count} salary levels", updated);
        }
        else
        {
            _logger.LogInformation("No salary levels required updating");
        }

        return updated;
    }


    private readonly ILogger<SalaryLevelUpdateProcessor> _logger;
    private readonly ApplicationDbContext _dbContext;
}
