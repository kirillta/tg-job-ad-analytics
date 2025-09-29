using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// One-off backfill service to populate Ad.StackId based on chat->stack mapping.
/// Produces unmapped_jobads.csv with chatId,count for missing mappings.
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
            .Join(_dbContext.Chats, am => am.TelegramChatId, c => c.TelegramId, (am, c) => new { am.Id, am.TelegramChatId, am.MessageId, c.Name })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return 0;

        var updated = 0;
        foreach (var chunk in items.Chunk(500))
        {
            foreach (var row in chunk)
            {
                if (!_resolver.TryResolve(row.TelegramChatId, out var stackId))
                    continue;

                await _dbContext.Ads
                    .Where(a => a.Id == row.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.StackId, stackId), cancellationToken);

                updated++;
            }
        }

        _logger.LogInformation("Backfill completed. Updated: {Updated}", updated);
        return updated;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StackBackfillService> _logger;
    private readonly ChannelStackResolver _resolver;
}
