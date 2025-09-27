using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// One-off backfill service to populate Ad.StackId based on channel mapping.
/// Produces unmapped_jobads.csv with channelName,count for missing mappings.
/// </summary>
public sealed class StackBackfillService
{
    public StackBackfillService(ILogger<StackBackfillService> logger, ApplicationDbContext dbContext, ChannelStackResolver resolver)
    {
        _logger = logger;
        _dbContext = dbContext;
        _resolver = resolver;
    }


    public async Task<int> BackfillMissingAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Ads
            .IgnoreQueryFilters()
            .Where(a => a.StackId == null)
            .Join(_dbContext.Messages, a => a.MessageId, m => m.Id, (a, m) => new { a.Id, a.MessageId, m.TelegramChatId })
            .Join(_dbContext.Chats, am => am.TelegramChatId, c => c.TelegramId, (am, c) => new { am.Id, am.MessageId, c.Name })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return 0;

        var unmappedByChannel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        foreach (var chunk in items.Chunk(500))
        {
            foreach (var row in chunk)
            {
                if (!_resolver.TryResolve(row.Name, out var stackId))
                    throw new InvalidOperationException($"Channel should be mapped: {row.Name}");

                await _dbContext.Ads
                    .Where(a => a.Id == row.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.StackId, stackId), cancellationToken);

                updated++;
            }
        }

        _logger.LogInformation("Backfill completed. Updated: {Updated}, Unmapped channels: {UnmappedChannels}", updated);
        return updated;
    }

    
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StackBackfillService> _logger;
    private readonly ChannelStackResolver _resolver;
}
