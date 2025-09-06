using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Levels;

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
    public SalaryLevelUpdateProcessor(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, PositionLevelResolver positionLevelResolver)
    {
        _logger = loggerFactory.CreateLogger<SalaryLevelUpdateProcessor>();

        _dbContext = dbContext;
        _positionLevelResolver = positionLevelResolver;
    }


    /// <summary>
    /// Updates salary level for all salaries where the level is <see cref="PositionLevel.Unknown"/>.
    /// </summary>
    /// <returns>The number of salary records updated.</returns>
    public async Task<int> UpdateMissingLevels()
    {
        _logger.LogInformation("Starting salary level update process");

        var items = await _dbContext.Salaries
            .Where(s => s.Level == PositionLevel.Unknown)
            .Join(_dbContext.Ads, s => s.AdId, a => a.Id, (s, a) => new { SalaryId = s.Id, a.MessageId, a.Text })
            .Join(_dbContext.Messages, sa => sa.MessageId, m => m.Id, (sa, m) => new { sa.SalaryId, sa.Text, m.Tags })
            .ToListAsync();

        _logger.LogInformation("Found {Count} salaries with unknown level", items.Count);

        if (items.Count == 0)
        {
            _logger.LogInformation("No salaries with unknown level found");
            return 0;
        }

        var totalUpdated = 0;
        var processed = 0;

        foreach (var chunk in items.Chunk(50))
        {
            var chunkUpdates = new List<(Guid Id, PositionLevel Level)>();
            foreach (var item in chunk)
            {
                var level = await _positionLevelResolver.Resolve(item.Tags, item.Text);
                if (level == PositionLevel.Unknown)
                    continue;

                chunkUpdates.Add((item.SalaryId, level));
            }

            processed += chunk.Length;
            if (chunkUpdates.Count > 0)
            {
                foreach (var group in chunkUpdates.GroupBy(u => u.Level))
                {
                    var level = group.Key;
                    var ids = group.Select(g => g.Id).Distinct().ToList();
                    foreach (var idChunk in ids.Chunk(500))
                    {
                        var updated = await _dbContext.Salaries
                            .Where(s => idChunk.Contains(s.Id))
                            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Level, level));

                        totalUpdated += updated;
                    }
                }
            }

            Console.Write(string.Empty);
            _logger.LogInformation("Processed {Processed}/{Total} items; total updated so far: {Updated}", processed, items.Count, totalUpdated);
        }

        _logger.LogInformation("Updated {Count} salary levels", totalUpdated);
        return totalUpdated;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryLevelUpdateProcessor> _logger;
    private readonly PositionLevelResolver _positionLevelResolver;
}
