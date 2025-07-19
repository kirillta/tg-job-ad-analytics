using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;

namespace TgJobAdAnalytics.Services.Uploads;

public class MessageDataService
{
    public MessageDataService(ILogger<MessageDataService> logger, ApplicationDbContext dbContext, IOptions<UploadOptions> options, IOptions<ParallelOptions> parallelOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _options = options.Value;
        _parallelOptions = parallelOptions.Value;
    }


    public async Task CleanData()
    {
        _logger.LogInformation("Cleaning all message data...");
        await _dbContext.Messages.ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("All message data has been removed");
    }


    public async Task Update(TgChat chat, UploadedDataState state, DateTime timeStamp)
    {
        switch (state)
        {
            case UploadedDataState.New:
                await Add(chat, timeStamp);
                break;
            case UploadedDataState.Existing:
                await AddOnlyNew(chat, timeStamp);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        };
    }


    private Task Add(TgChat chat, DateTime timeStamp) 
        => ProcessInternal(chat, chat.Messages, timeStamp);


    private async Task AddOnlyNew(TgChat chat, DateTime timeStamp)
    {
        var existingMessageTelegramIds = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .Select(m => m.TelegramMessageId)
            .ToHashSetAsync();

        var targetMessages = chat.Messages
            .Where(m => !existingMessageTelegramIds.Contains(m.Id))
            .ToList();            
            
        await ProcessInternal(chat, targetMessages, timeStamp);
    }


    private async Task ProcessInternal(TgChat chat, List<TgMessage> messages, DateTime timeStamp)
    {
        var entryBag = new ConcurrentBag<MessageEntity>();
        Parallel.ForEach(messages, _parallelOptions, tgMessage =>
        {
            var textEntries = ToRawEntries(tgMessage.TextEntities);
            if (textEntries.Count == 0)
                return;
            var tags = ToRawTags(tgMessage.TextEntities);
            if (tags.Count == 0)
                return;

            entryBag.Add(new MessageEntity
            {
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
            int currentBatchSize = Math.Min(batchSize, entries.Count - i);
            var batch = entries.GetRange(i, currentBatchSize);
        
            await _dbContext.Messages.AddRangeAsync(batch);
            await _dbContext.SaveChangesAsync();

            addedCount += currentBatchSize;
        }
            
        _logger.LogInformation("Added {AddedCount} messages to the database", addedCount);


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


    private readonly ILogger<MessageDataService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly UploadOptions _options;
    private readonly ParallelOptions _parallelOptions;
}
