using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

/// <summary>
/// Persists advertisement entries derived from Telegram chat messages. Handles full or incremental ingestion modes
/// (based on <see cref="UploadedDataState"/>) and batches inserts to the database. During persistence it computes
/// MinHash signatures for deduplication / similarity workflows and stages both vector store and LSH index entities.
/// </summary>
public sealed class TelegramAdPersistenceService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramAdPersistenceService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="dbContext">EF Core database context.</param>
    /// <param name="parallelOptions">Parallel execution options for message-to-ad transformation.</param>
    /// <param name="uploadOptions">Upload configuration (batch size, mode, etc.).</param>
    /// <param name="minHashVectorizer">Vectorizer used to compute MinHash signatures for ad texts.</param>
    /// <param name="vectorStore">Persistent store for raw MinHash signatures.</param>
    /// <param name="vectorIndex">Persistent LSH index for similarity querying.</param>
    /// <param name="channelStackResolverFactory">Factory resolving technology stack ids for channels.</param>
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


    /// <summary>
    /// Deletes all persisted advertisement entities (ignores query filters) and commits the change.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning all ad data...");

        await _dbContext.Ads.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("All ad data has been removed.");
    }


    /// <summary>
    /// Upserts advertisements for the specified chat according to the upload state: full ingest for <see cref="UploadedDataState.New"/>,
    /// or incremental ingest adding only previously unseen message-derived ads for <see cref="UploadedDataState.Existing"/>.
    /// </summary>
    /// <param name="chat">Telegram chat payload containing messages.</param>
    /// <param name="state">Ingestion mode indicating whether data is new or incremental.</param>
    /// <param name="timeStamp">Timestamp applied to created/updated fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of new advertisements persisted.</returns>
    public async Task<int> Upsert(TgChat chat, UploadedDataState state, DateTime timeStamp, CancellationToken cancellationToken) 
        => state switch
        {
            UploadedDataState.New => await AddAll(chat, timeStamp, cancellationToken),
            UploadedDataState.Existing => await AddOnlyNew(chat, timeStamp, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };


    private async Task<int> AddAll(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var pageSize = _uploadOptions.BatchSize;
        var offset = 0;
        var totalAdded = 0;

        while (true)
        {
            var messages = await _dbContext.Messages
                .Where(m => m.TelegramChatId == chat.Id)
                .OrderBy(m => m.Id)
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
                break;

            totalAdded += await ProcessAndInsert(chat, messages, timeStamp, cancellationToken, skipExistenceCheck: true);

            if (messages.Count < pageSize)
                break;

            offset += pageSize;
        }

        return totalAdded;
    }


    private async Task<int> AddOnlyNew(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var diff = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id && !_dbContext.Ads.IgnoreQueryFilters().Any(a => a.MessageId == m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (diff.Count == 0)
            return 0;

        var pageSize = _uploadOptions.BatchSize;
        var totalAdded = 0;

        for (var offset = 0; offset < diff.Count; offset += pageSize)
        {
            var page = diff.GetRange(offset, Math.Min(pageSize, diff.Count - offset));
            var messages = await _dbContext.Messages
                .Where(m => page.Contains(m.Id))
                .ToListAsync(cancellationToken);

            totalAdded += await ProcessAndInsert(chat, messages, timeStamp, cancellationToken);
        }

        return totalAdded;
    }


    private async Task<int> ProcessAndInsert(TgChat chat, List<MessageEntity> messages, DateTime timeStamp, CancellationToken cancellationToken, bool skipExistenceCheck = false)
    {
        _logger.LogInformation($"Processing {messages.Count} messages from chat {chat.Name}");

        var resolver = await _channelStackResolverFactory.Create();

        var entries = new List<AdEntity>();
        var entriesLock = new Lock();
        Parallel.ForEach(messages, _parallelOptions, () => (List: new List<AdEntity>(), Builder: new StringBuilder()), (message, _, localState) =>
            {
                if (!IsProcessable(message, timeStamp))
                    return localState;

                var normalizedText = GetText(message, localState.Builder);
                if (string.IsNullOrEmpty(normalizedText))
                    return localState;

                if (!resolver.TryResolve(chat.Id, out var stackId))
                {
                    _logger.LogCritical($"Unknown channel for stack mapping. channelName={chat.Name} messageId={message.Id}");
                    return localState;
                }

                var adId = DeterministicGuid.Create(Namespaces.Ads, $"{message.TelegramChatId}:{message.TelegramMessageId}:ad");

                localState.List.Add(new AdEntity
                {
                    Id = adId,
                    Date = DateOnly.FromDateTime(message.TelegramMessageDate),
                    Text = normalizedText,
                    MessageId = message.Id,
                    StackId = stackId,
                    CreatedAt = timeStamp,
                    UpdatedAt = timeStamp
                });

                return localState;
            }, localState =>
            {
                lock (entriesLock)
                    entries.AddRange(localState.List);
            });

        var batchSize = _uploadOptions.BatchSize;
        var addedCount = 0;
        for (var i = 0; i < entries.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int currentBatchSize = Math.Min(batchSize, entries.Count - i);
            var batch = entries.GetRange(i, currentBatchSize);

            List<AdEntity> newBatch;
            if (skipExistenceCheck)
            {
                newBatch = batch;
            }
            else
            {
                var batchIds = batch.Select(e => e.Id).ToList();
                var existingIds = await _dbContext.Ads
                    .IgnoreQueryFilters()
                    .Where(a => batchIds.Contains(a.Id))
                    .Select(a => a.Id)
                    .ToHashSetAsync(cancellationToken);

                newBatch = batch.Where(e => !existingIds.Contains(e.Id))
                    .ToList();
            }

            if (newBatch.Count == 0)
                continue;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.Ads.AddRange(newBatch);

            var vectorStoreItems = new List<(Guid AdId, uint[] Signature, int ShingleCount)>(newBatch.Count);
            var vectorIndexItems = new List<(Guid AdId, uint[] Signature)>(newBatch.Count);

            foreach (var ad in newBatch)
            {
                var (signature, shingleCount) = _minHashVectorizer.GenerateMinHashSignature(ad.Text);
                vectorStoreItems.Add((ad.Id, signature, shingleCount));
                vectorIndexItems.Add((ad.Id, signature));
            }

            await _vectorStore.UpsertBatchWithoutSave(vectorStoreItems, timeStamp, cancellationToken);
            await _vectorIndex.UpsertBatchWithoutSave(vectorIndexItems, timeStamp, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            addedCount += newBatch.Count;
            _logger.LogInformation($"Batch added {newBatch.Count} ads into the database");
        }

        _logger.LogInformation($"Added {addedCount} ads to the database");
        return addedCount;


        string GetText(MessageEntity message, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
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
