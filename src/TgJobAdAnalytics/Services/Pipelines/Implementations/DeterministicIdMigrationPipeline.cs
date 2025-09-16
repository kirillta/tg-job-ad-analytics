using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Utils;

namespace TgJobAdAnalytics.Services.Pipelines.Implementations;

/// <summary>
/// Migrates existing Message and Ad identifiers to deterministic GUIDs derived from Telegram keys.
/// Also updates dependent tables to keep referential consistency.
/// </summary>
public sealed class DeterministicIdMigrationPipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicIdMigrationPipeline"/> class.
    /// </summary>
    public DeterministicIdMigrationPipeline(ILoggerFactory loggerFactory, ApplicationDbContext dbContext)
    {
        _logger = loggerFactory.CreateLogger<DeterministicIdMigrationPipeline>();
        _dbContext = dbContext;
    }


    /// <inheritdoc/>
    public string Name 
        => "migrate-deterministic-ids";


    /// <inheritdoc/>
    public string Description 
        => "Migrate Messages and Ads to deterministic IDs (based on Telegram chat/message ids) and update dependent tables.";


    /// <inheritdoc/>
    public bool IsIdempotent 
        => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Select(m => new { m.Id, m.TelegramChatId, m.TelegramMessageId })
            .ToListAsync(cancellationToken);

        var messageMap = new List<(Guid OldId, Guid NewId)>();
        foreach (var message in messages)
        {
            var newId = DeterministicGuid.Create(Namespaces.Messages, $"{message.TelegramChatId}:{message.TelegramMessageId}");
            if (newId != message.Id)
                messageMap.Add((message.Id, newId));
        }

        var adInputs = await _dbContext.Ads
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Join(_dbContext.Messages.AsNoTracking(), a => a.MessageId, m => m.Id, (a, m) => new
            {
                AdId = a.Id,
                a.MessageId,
                m.TelegramChatId,
                m.TelegramMessageId
            })
            .ToListAsync(cancellationToken);

        var adMap = new List<(Guid OldId, Guid NewId)>();
        foreach (var adInput in adInputs)
        {
            var newAdId = DeterministicGuid.Create(Namespaces.Ads, $"{adInput.TelegramChatId}:{adInput.TelegramMessageId}:ad");
            if (newAdId != adInput.AdId)
                adMap.Add((adInput.AdId, newAdId));
        }

        if (messageMap.Count == 0 && adMap.Count == 0)
        {
            _logger.LogInformation("DeterministicIdMigration: nothing to migrate");
            return 0;
        }

        var updated = 0;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1) Update Messages.Id
            foreach (var (oldId, newId) in messageMap)
            {
                var affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Messages\" SET \"Id\" = {0} WHERE \"Id\" = {1}",
                    newId, oldId);
                updated += affected;
            }

            // 2) Update Ads.MessageId to point to the new message ids
            foreach (var (oldId, newId) in messageMap)
            {
                var affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Ads\" SET \"MessageId\" = {0} WHERE \"MessageId\" = {1}",
                    newId, oldId);
                updated += affected;
            }

            // 3) Update Ads.Id
            foreach (var (oldId, newId) in adMap)
            {
                var exists = await _dbContext.Ads.IgnoreQueryFilters().AnyAsync(a => a.Id == newId, cancellationToken);
                if (exists)
                {
                    _logger.LogWarning("DeterministicIdMigration: target AdId {NewId} already exists; skipping update from {OldId}", newId, oldId);
                    continue;
                }

                var affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Ads\" SET \"Id\" = {0} WHERE \"Id\" = {1}",
                    newId, oldId);
                updated += affected;

                // 4) Update dependents for this ad id
                affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Salaries\" SET \"AdId\" = {0} WHERE \"AdId\" = {1}",
                    newId, oldId);
                updated += affected;

                affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"AdVectors\" SET \"AdId\" = {0} WHERE \"AdId\" = {1}",
                    newId, oldId);
                updated += affected;

                affected = await _dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE \"LshBuckets\" SET \"AdId\" = {0} WHERE \"AdId\" = {1}",
                    newId, oldId);
                updated += affected;
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "DeterministicIdMigration: failed with error {Message}", ex.Message);
            throw;
        }

        _logger.LogInformation("DeterministicIdMigration: updated {Updated} rows (Messages: {MsgCount}, Ads: {AdCount})", updated, messageMap.Count, adMap.Count);
        return updated;
    }

    
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeterministicIdMigrationPipeline> _logger;
}
