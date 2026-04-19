using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Uploads
{
    /// <summary>
    /// Coordinates importing Telegram chat JSON exports into the persistence layer. Handles conditional cleaning,
    /// incremental vs. full ingestion based on <see cref="UploadOptions"/>, persists chats, messages and derived ads,
    /// then optionally performs post-import ad deduplication using the <see cref="SimilarityCalculator"/>.
    /// </summary>
    public sealed class TelegramChatImportService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TelegramChatImportService"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="dbContext">EF Core application database context.</param>
        /// <param name="options">Upload configuration options (mode, batch size, source path).</param>
        /// <param name="scopeFactory">Factory used to create per-file DI scopes for isolated <see cref="ApplicationDbContext"/> instances.</param>
        /// <param name="telegramAdPersistenceService">Service used to persist advertisement entities.</param>
        /// <param name="telegramChatPersistenceService">Service used to persist chat metadata.</param>
        /// <param name="telegramMessagePersistenceService">Service used to persist raw messages and text entries.</param>
        /// <param name="similarityCalculator">Calculator used to mark unique ads after import.</param>
        public TelegramChatImportService(
            ILogger<TelegramChatImportService> logger,
            ApplicationDbContext dbContext,
            IOptions<UploadOptions> options,
            IServiceScopeFactory scopeFactory,
            TelegramAdPersistenceService telegramAdPersistenceService,
            TelegramChatPersistenceService telegramChatPersistenceService,
            TelegramMessagePersistenceService telegramMessagePersistenceService,
            SimilarityCalculator similarityCalculator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _telegramAdPersistenceService = telegramAdPersistenceService;
            _telegramChatPersistenceService = telegramChatPersistenceService;
            _telegramMessagePersistenceService = telegramMessagePersistenceService;
            _options = options.Value;
            _similarityCalculator = similarityCalculator;
        }


        /// <summary>
        /// Imports all Telegram chat JSON files from the configured source path. Applies the configured upload mode
        /// (skip, clean, incremental), persists new or updated chats/messages/ads, and deduplicates newly added ads.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<int> ImportFromJson(CancellationToken cancellationToken)
        {
            if (_options.Mode == UploadMode.Skip)
            {
                _logger.LogInformation("Update skipped due to configuration settings");
                return 0;
            }

            if (_options.Mode == UploadMode.Clean)
            {
                await _telegramChatPersistenceService.RemoveAll(cancellationToken);
                await _telegramMessagePersistenceService.RemoveAll(cancellationToken);
                // Preserve ads and derived data (salaries, vectors) to avoid re-processing LLM and vectorization work
                //_ = await _telegramAdPersistenceService.RemoveAll();
            }

            var timeStamp = DateTime.UtcNow;
            var jsonFiles = Directory.GetFiles(_options.SourcePath)
                .Where(f => f.EndsWith(".json"))
                .ToList();

            var totalAddedAds = 0;
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount };
            await Parallel.ForEachAsync(jsonFiles, parallelOptions, async (fileName, ct) =>
            {
                var chatProcessingTime = Stopwatch.GetTimestamp();
                var chatFileName = Path.GetFileName(fileName);
                _logger.LogInformation("Processing file: {FileName}", chatFileName);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var adService = scope.ServiceProvider.GetRequiredService<TelegramAdPersistenceService>();
                var chatService = scope.ServiceProvider.GetRequiredService<TelegramChatPersistenceService>();
                var messageService = scope.ServiceProvider.GetRequiredService<TelegramMessagePersistenceService>();

                var addedAds = await Process(fileName, adService, chatService, messageService, ct);
                Interlocked.Add(ref totalAddedAds, addedAds);

                _logger.LogInformation("File {FileName} processed in {ElapsedSeconds} seconds", chatFileName, Stopwatch.GetElapsedTime(chatProcessingTime).TotalSeconds);
            });

            _logger.LogInformation("Chat processing completed");

            if (totalAddedAds > 0)
            {
                _logger.LogInformation("Deduplicating {TotalAddedAds} new ads", totalAddedAds);
                await DeduplicateAds(cancellationToken);
            }
            else
            {
                _logger.LogInformation("No new ads added. Skipping deduplication.");
            }

            return totalAddedAds;


            async Task<int> Process(
                string fileName,
                TelegramAdPersistenceService adService,
                TelegramChatPersistenceService chatService,
                TelegramMessagePersistenceService messageService,
                CancellationToken ct)
            {
                var chat = await ReadChatFromFile(fileName, ct);
                var chatState = await chatService.DetermineState(chat, ct);

                var addedMessages = await messageService.Upsert(chat, chatState, timeStamp, ct);
                var addedAds = await adService.Upsert(chat, chatState, timeStamp, ct);

                if (chatState == UploadedDataState.New || addedMessages > 0)
                    await chatService.Upsert(chat, chatState, timeStamp, ct);
                else
                    _logger.LogInformation("No new messages for chat '{ChatName}'. Skipping chat update.", chat.Name);

                return addedAds;
            }


            static async Task<TgChat> ReadChatFromFile(string name, CancellationToken cancellationToken)
            {
                await using var stream = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
                return await JsonSerializer.DeserializeAsync<TgChat>(stream, cancellationToken: cancellationToken);
            }
        }


        private async Task DeduplicateAds(CancellationToken cancellationToken)
        {
            var ads = await _dbContext.Ads
                .AsNoTracking()
                .IgnoreQueryFilters()
                .OrderByDescending(a => a.Date)
                .Select(a => new Data.Messages.AdEntity { Id = a.Id, Date = a.Date })
                .ToListAsync(cancellationToken);

            var uniqueAds = await _similarityCalculator.DistinctPersistent(ads, cancellationToken);
            _logger.LogInformation("Found {UniqueAdCount} unique ads out of {TotalAdCount}", uniqueAds.Count, ads.Count);

            var uniqueIds = uniqueAds.Select(a => a.Id).ToList();
            var updatedAt = DateTime.UtcNow;

            const int batchSize = 500;
            for (var i = 0; i < uniqueIds.Count; i += batchSize)
            {
                var batch = uniqueIds.Skip(i).Take(batchSize).ToList();
                await _dbContext.Ads
                    .IgnoreQueryFilters()
                    .Where(ad => batch.Contains(ad.Id))
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(a => a.IsUnique, true)
                        .SetProperty(a => a.UpdatedAt, updatedAt), cancellationToken);
            }
        }


        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TelegramChatImportService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TelegramAdPersistenceService _telegramAdPersistenceService;
        private readonly TelegramChatPersistenceService _telegramChatPersistenceService;
        private readonly TelegramMessagePersistenceService _telegramMessagePersistenceService;
        private readonly UploadOptions _options;
        private readonly SimilarityCalculator _similarityCalculator;
    }
}