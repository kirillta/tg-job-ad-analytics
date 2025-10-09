using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// One-off backfill service that populates missing <c>Ad.StackId</c> values based on the current channel?stack mapping.
/// For each ad without a stack it looks up the originating chat's mapped technology stack and updates the ad in place.
/// Logs the number of updated rows; intended for maintenance / migration scenarios.
/// </summary>
public sealed class StackBackfillService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StackBackfillService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="resolverFactory">Factory used to create a channel stack resolver.</param>
    public StackBackfillService(ILogger<StackBackfillService> logger, ApplicationDbContext dbContext, ChannelStackResolverFactory resolverFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _resolverFactory = resolverFactory;
    }


    /// <summary>
    /// Backfills missing <c>StackId</c> values for ads lacking a stack association.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of ads updated.</returns>
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

        var resolver = await _resolverFactory.Create();
        var updated = 0;
        foreach (var chunk in items.Chunk(500))
        {
            foreach (var row in chunk)
            {
                if (!resolver.TryResolve(row.TelegramChatId, out var stackId))
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
    private readonly ChannelStackResolverFactory _resolverFactory;
}
