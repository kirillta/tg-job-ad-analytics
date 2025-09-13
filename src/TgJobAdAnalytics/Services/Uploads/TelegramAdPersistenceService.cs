using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Services.Uploads;

public class TelegramAdPersistenceService
{
    public TelegramAdPersistenceService(
        ILogger<TelegramAdPersistenceService> logger,
        ApplicationDbContext dbContext,
        IOptions<ParallelOptions> parallelOptions,
        IOptions<UploadOptions> uploadOptions,
        IMinHashVectorizer minHashVectorizer,
        IVectorStore vectorStore,
        IVectorIndex vectorIndex)
    {
        _logger = logger;
        _dbContext = dbContext;
        _parallelOptions = parallelOptions.Value;
        _uploadOptions = uploadOptions.Value;
        _minHashVectorizer = minHashVectorizer;
        _vectorStore = vectorStore;
        _vectorIndex = vectorIndex;
    }


    public async Task RemoveAll()
    {
        _logger.LogInformation("Cleaning all ad data...");
        await _dbContext.Ads.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("All ad data has been removed.");
    }


    public async Task Upsert(TgChat chat, UploadedDataState state, DateTime timeStamp)
    {
        switch (state)
        {
            case UploadedDataState.New:
                await AddAll(chat, timeStamp);
                break;
            case UploadedDataState.Existing:
                await AddOnlyNew(chat, timeStamp);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }


    private async Task AddAll(TgChat chat, DateTime timeStamp)
    {
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .ToListAsync();

        await ProcessAndInsert(messages, timeStamp);
    }


    private async Task AddOnlyNew(TgChat chat, DateTime timeStamp)
    {
        var existingMessageIds = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .Select(m => m.Id)
            .ToHashSetAsync();

        var existingAdMessageIds = await _dbContext.Ads
            .IgnoreQueryFilters()
            .Where(a => existingMessageIds.Contains(a.MessageId))
            .Select(a => a.MessageId)
            .ToHashSetAsync();

        var diff = existingMessageIds.Except(existingAdMessageIds);
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id && diff.Contains(m.Id))
            .ToListAsync();

        await ProcessAndInsert(messages, timeStamp);
    }


    private async Task ProcessAndInsert(List<MessageEntity> messages, DateTime timeStamp)
    {
        var entryBag = new ConcurrentBag<AdEntity>();
        Parallel.ForEach(messages, _parallelOptions, message =>
        {
            if (!IsProcessable(message, timeStamp))
                return;

            var normalizedText = GetText(message);
            if (string.IsNullOrEmpty(normalizedText))
                return;

            entryBag.Add(new AdEntity
            {
                Date = DateOnly.FromDateTime(message.TelegramMessageDate),
                Text = normalizedText,
                MessageId = message.Id,
                CreatedAt = timeStamp,
                UpdatedAt = timeStamp
            });
        });

        var entries = entryBag.ToList();
        var batchSize = _uploadOptions.BatchSize;
        var addedCount = 0;
        for (int i = 0; i < entries.Count; i += batchSize)
        {
            int currentBatchSize = Math.Min(batchSize, entries.Count - i);
            var batch = entries.GetRange(i, currentBatchSize);

            await _dbContext.Ads.AddRangeAsync(batch);
            await _dbContext.SaveChangesAsync();

            foreach (var ad in batch)
            {
                var (signature, shingleCount) = _minHashVectorizer.Compute(ad.Text);

                await _vectorStore.Upsert(ad.Id, signature, shingleCount, CancellationToken.None);
                await _vectorIndex.Upsert(ad.Id, signature, CancellationToken.None);
            }

            addedCount += currentBatchSize;
        }

        _logger.LogInformation("Added {AddedCount} ads to the database", addedCount);


        string GetText(MessageEntity message)
        {
            var stringBuilder = new StringBuilder();
            foreach (var entity in message.TextEntries)
            {
                if (entity.Key is TgTextEntryType.PlainText)
                { 
                    var normalizedText = TextNormalizer.NormalizeTextEntry(entity.Value);
                    stringBuilder.Append(normalizedText);
                }
                else
                {
                    stringBuilder.Append(entity.Value);
                }

                stringBuilder.Append(' ');
            }

            if (stringBuilder.Length < MinimalValuebleMessageLength)
                return string.Empty;

            return TextNormalizer.NormalizeAdText(stringBuilder.ToString());
        }


        bool IsProcessable(MessageEntity message, DateTime timeStamp)
        {
            if (message.TextEntries.Count == 0)
                return false;

            if (message.Tags.Count == 0)
                return false;

            var hashTags = message.TextEntries
                .Where(entity => entity.Key is TgTextEntryType.HashTag)
                .Select(entity => entity.Value)
                .ToList();

            if (IsJob())
                return false;

            if (!IsAd())
                return false;

            return true;


            bool IsAd() 
                => hashTags.Any(AdTags.Contains);


            bool IsJob() 
                => hashTags.Any(JobTags.Contains);
        }
    }


    private static readonly HashSet<string> JobTags = 
    [
        "#резюме",
        "#ищу"
    ];

    private static readonly HashSet<string> AdTags =
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
        "#fulltime",
        "#удаленка",
        "#офис",
        "#remote",
        "#удалёнка",
        "#удаленно",
        "#parttime",
        "#гибрид",
        "#work",
        "#удаленнаяработа",
        "#office",
        "#релокация",
        "#relocation",
        "#relocate",
        "#полная",
        "#фуллтайм"
    ];

    private const int MinimalValuebleMessageLength = 300;

    private readonly ILogger<TelegramAdPersistenceService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly ParallelOptions _parallelOptions;
    private readonly UploadOptions _uploadOptions;
    private readonly IMinHashVectorizer _minHashVectorizer;
    private readonly IVectorStore _vectorStore;
    private readonly IVectorIndex _vectorIndex;
}
