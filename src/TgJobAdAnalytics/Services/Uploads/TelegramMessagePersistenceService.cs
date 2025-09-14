using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Utils;

namespace TgJobAdAnalytics.Services.Uploads;

public class TelegramMessagePersistenceService
{
    public TelegramMessagePersistenceService(ILogger<TelegramMessagePersistenceService> logger, ApplicationDbContext dbContext, IOptions<UploadOptions> options, IOptions<ParallelOptions> parallelOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _options = options.Value;
        _parallelOptions = parallelOptions.Value;
    }


    public async Task RemoveAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning all message data...");
        await _dbContext.Messages.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All message data has been removed");
    }


    public async Task<int> Upsert(TgChat chat, UploadedDataState state, DateTime timeStamp, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UploadedDataState.New:
                return await AddAll(chat, timeStamp, cancellationToken);
            case UploadedDataState.Existing:
                return await AddOnlyNew(chat, timeStamp, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        };
    }


    private Task<int> AddAll(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
        => ProcessAndInsert(chat, chat.Messages, timeStamp, cancellationToken);


    private async Task<int> AddOnlyNew(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var existingMessageTelegramIds = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .Select(m => m.TelegramMessageId)
            .ToHashSetAsync(cancellationToken);

        var targetMessages = chat.Messages
            .Where(m => !existingMessageTelegramIds.Contains(m.Id))
            .ToList();            
            
        return await ProcessAndInsert(chat, targetMessages, timeStamp, cancellationToken);
    }


    private async Task<int> ProcessAndInsert(TgChat chat, List<TgMessage> messages, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var entryBag = new ConcurrentBag<MessageEntity>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _parallelOptions.MaxDegreeOfParallelism
        };

        Parallel.ForEach(messages, parallelOptions, tgMessage =>
        {
            var textEntries = ToRawEntries(tgMessage.TextEntities);
            if (textEntries.Count == 0)
                return;
            var tags = ToRawTags(tgMessage.TextEntities);
            if (tags.Count == 0)
                return;

            var deterministicId = DeterministicGuid.Create(Namespaces.Messages, $"{chat.Id}:{tgMessage.Id}");

            entryBag.Add(new MessageEntity
            {
                Id = deterministicId,
                TelegramChatId = chat.Id,
                TelegramMessageId = tgMessage.Id,
                TelegramMessageDate = tgMessage.Date,
                TextEntries = textEntries,
                Tags = tags,
                CreatedAt = timeStamp,
                UpdatedAt = timeStamp
            });
        });

        var entries = entryBag.ToList();
        var batchSize = _options.BatchSize;
        var addedCount = 0;
        for (int i = 0; i < entries.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int currentBatchSize = Math.Min(batchSize, entries.Count - i);
            var batch = entries.GetRange(i, currentBatchSize);

            var batchIds = batch.Select(e => e.Id).ToList();
            var existingIds = await _dbContext.Messages
                .Where(m => batchIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToHashSetAsync(cancellationToken);

            var newBatch = batch.Where(e => !existingIds.Contains(e.Id)).ToList();
            if (newBatch.Count == 0)
                continue;
        
            await _dbContext.Messages.AddRangeAsync(newBatch, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            addedCount += newBatch.Count;
        }
            
        _logger.LogInformation("Added {AddedCount} messages to the database", addedCount);

        return addedCount;


        static List<string> ToRawTags(List<TgTextEntry> entries)
        {
            var results = new List<string>(entries.Count);
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Text))
                    continue;

                if (entry.Type == TgTextEntryType.HashTag)
                    results.Add(entry.Text.ToLowerInvariant());
            }

            return results;
        }


        static List<KeyValuePair<TgTextEntryType, string>> ToRawEntries(List<TgTextEntry> entries)
        {
            var results = new List<KeyValuePair<TgTextEntryType, string>>(entries.Count);
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Text))
                    continue;

                results.Add(new KeyValuePair<TgTextEntryType, string>(entry.Type, entry.Text));
            }

            return results;
        }
    }


    private readonly ILogger<TelegramMessagePersistenceService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly UploadOptions _options;
    private readonly ParallelOptions _parallelOptions;
}
