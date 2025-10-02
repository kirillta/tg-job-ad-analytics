using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Telegram.Enums;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Stacks;
using TgJobAdAnalytics.Services.Vectors;
using TgJobAdAnalytics.Utils;

namespace TgJobAdAnalytics.Services.Uploads;

public sealed class TelegramAdPersistenceService
{
    public TelegramAdPersistenceService(
        ILogger<TelegramAdPersistenceService> logger,
        ApplicationDbContext dbContext,
        IOptions<ParallelOptions> parallelOptions,
        IOptions<UploadOptions> uploadOptions,
        MinHashVectorizer minHashVectorizer,
        VectorStore vectorStore,
        VectorIndex vectorIndex,
        ChannelStackResolverFactory channelStackResolverFactory)
    {
        _channelStackResolverFactory = channelStackResolverFactory;
        _dbContext = dbContext;
        _logger = logger;
        _minHashVectorizer = minHashVectorizer;
        _parallelOptions = parallelOptions.Value;
        _uploadOptions = uploadOptions.Value;
        _vectorIndex = vectorIndex;
        _vectorStore = vectorStore;
    }


    public async Task RemoveAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning all ad data...");
        await _dbContext.Ads.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All ad data has been removed.");
    }


    public async Task Upsert(TgChat chat, UploadedDataState state, DateTime timeStamp, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UploadedDataState.New:
                await AddAll(chat, timeStamp, cancellationToken);
                break;
            case UploadedDataState.Existing:
                await AddOnlyNew(chat, timeStamp, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }


    private async Task AddAll(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .ToListAsync(cancellationToken);

        await ProcessAndInsert(chat, messages, timeStamp, cancellationToken);
    }


    private async Task AddOnlyNew(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var existingMessageIds = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .Select(m => m.Id)
            .ToHashSetAsync(cancellationToken);

        var existingAdMessageIds = await _dbContext.Ads
            .IgnoreQueryFilters()
            .Where(a => existingMessageIds.Contains(a.MessageId))
            .Select(a => a.MessageId)
            .ToHashSetAsync(cancellationToken);

        var diff = existingMessageIds.Except(existingAdMessageIds);
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id && diff.Contains(m.Id))
            .ToListAsync(cancellationToken);

        await ProcessAndInsert(chat, messages, timeStamp, cancellationToken);
    }


    private async Task ProcessAndInsert(TgChat chat, List<MessageEntity> messages, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var resolver = await _channelStackResolverFactory.Create();

        var entryBag = new ConcurrentBag<AdEntity>();
        Parallel.ForEach(messages, _parallelOptions, async message =>
        {
            if (!IsProcessable(message, timeStamp))
                return;

            var normalizedText = GetText(message);
            if (string.IsNullOrEmpty(normalizedText))
                return;

            if (!resolver.TryResolve(chat.Id, out var stackId))
            {
                _logger.LogCritical("Unknown channel for stack mapping. channelName={ChannelName} messageId={MessageId}", chat.Name, message.Id);
                return;
            }

            var adId = DeterministicGuid.Create(Namespaces.Ads, $"{message.TelegramChatId}:{message.TelegramMessageId}:ad");

            entryBag.Add(new AdEntity
            {
                Id = adId,
                Date = DateOnly.FromDateTime(message.TelegramMessageDate),
                Text = normalizedText,
                MessageId = message.Id,
                StackId = stackId,
                CreatedAt = timeStamp,
                UpdatedAt = timeStamp
            });
        });

        var entries = entryBag.ToList();
        var batchSize = _uploadOptions.BatchSize;
        var addedCount = 0;
        for (int i = 0; i < entries.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int currentBatchSize = Math.Min(batchSize, entries.Count - i);
            var batch = entries.GetRange(i, currentBatchSize);

            var batchIds = batch.Select(e => e.Id).ToList();
            var existingIds = await _dbContext.Ads
                .IgnoreQueryFilters()
                .Where(a => batchIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToHashSetAsync(cancellationToken);

            var newBatch = batch.Where(e => !existingIds.Contains(e.Id)).ToList();
            if (newBatch.Count == 0)
                continue;

            await _dbContext.Ads.AddRangeAsync(newBatch, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var ad in newBatch)
            {
                var (signature, shingleCount) = _minHashVectorizer.Compute(ad.Text);
                await _vectorStore.Upsert(ad.Id, signature, shingleCount, cancellationToken);
                await _vectorIndex.Upsert(ad.Id, signature, cancellationToken);
            }

            addedCount += newBatch.Count;
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


    private readonly ChannelStackResolverFactory _channelStackResolverFactory;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TelegramAdPersistenceService> _logger;
    private readonly MinHashVectorizer _minHashVectorizer;
    private readonly ParallelOptions _parallelOptions;
    private readonly UploadOptions _uploadOptions;
    private readonly VectorIndex _vectorIndex;
    private readonly VectorStore _vectorStore;
}
