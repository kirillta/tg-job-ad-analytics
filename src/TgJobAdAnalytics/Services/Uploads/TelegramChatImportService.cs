using Microsoft.EntityFrameworkCore;
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
    public sealed class TelegramChatImportService
    {
        public TelegramChatImportService(
            ILogger<TelegramChatImportService> logger,
            ApplicationDbContext dbContext,
            IOptions<UploadOptions> options, 
            TelegramAdPersistenceService adDataService,
            TelegramChatPersistenceService telegramChatPersistenceService, 
            TelegramMessagePersistenceService telegramMessagePersistenceService,
            SimilarityCalculator similarityCalculator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _adDataService = adDataService;
            _telegramChatPersistenceService = telegramChatPersistenceService;
            _telegramMessagePersistenceService = telegramMessagePersistenceService;
            _options = options.Value;
            _similarityCalculator = similarityCalculator;
        }


        public async Task ImportFromJson(CancellationToken cancellationToken)
        {
            if (_options.Mode == UploadMode.Skip)
            {
                _logger.LogInformation("Update skipped due to configuration settings");
                return;
            }

            if (_options.Mode == UploadMode.Clean)
            {
                await _telegramChatPersistenceService.RemoveAll(cancellationToken);
                await _telegramMessagePersistenceService.RemoveAll(cancellationToken);
                // Preserve ads and derived data (salaries, vectors) to avoid re-processing LLM and vectorization work
                //_ = await _adDataService.RemoveAll();
            }

            var timeStamp = DateTime.UtcNow;
            var fileNames = Directory.GetFiles(_options.SourcePath);
            foreach (string fileName in fileNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileName.EndsWith(".json"))
                    continue;

                var chatProcessingTime = Stopwatch.GetTimestamp();
                var chatFileName = Path.GetFileName(fileName);
                _logger.LogInformation("Processing file: {FileName}", chatFileName);

                var chat = await ReadChatFromFile(fileName, cancellationToken);

                var chatState = await _telegramChatPersistenceService.DetermineState(chat, cancellationToken);

                var addedMessages = await _telegramMessagePersistenceService.Upsert(chat, chatState, timeStamp, cancellationToken);
                await _adDataService.Upsert(chat, chatState, timeStamp, cancellationToken);

                if (chatState == UploadedDataState.New || addedMessages > 0)
                    await _telegramChatPersistenceService.Upsert(chat, chatState, timeStamp, cancellationToken);
                else
                    _logger.LogInformation("No new messages for chat '{ChatName}'. Skipping chat update.", chat.Name);

                _logger.LogInformation("File {FileName} processed in {ElapsedSeconds} seconds", chatFileName, Stopwatch.GetElapsedTime(chatProcessingTime).TotalSeconds);
            }            
        
            _logger.LogInformation("Chat processing completed");
            await DeduplicateAds(cancellationToken);


            static async Task<TgChat> ReadChatFromFile(string name, CancellationToken cancellationToken)
            {
                using var json = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read);
                var buffer = new byte[json.Length];
                await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length), cancellationToken);

                return JsonSerializer.Deserialize<TgChat>(buffer);
            }
        }


        private async Task DeduplicateAds(CancellationToken cancellationToken)
        {
            var ads = await _dbContext.Ads
                .AsNoTracking()
                .IgnoreQueryFilters()
                .OrderByDescending(a => a.Date)
                .ToListAsync(cancellationToken);

            var uniqueAds = _similarityCalculator.Distinct(ads);
            _logger.LogInformation("Found {UniqueAdCount} unique ads out of {TotalAdCount}", uniqueAds.Count, ads.Count);

            foreach (var batch in uniqueAds.Chunk(_options.BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _dbContext.Ads
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(ad => batch.Select(b => b.Id).Contains(ad.Id))
                    .ExecuteUpdateAsync(b => b
                    .SetProperty(a => a.IsUnique, true)
                    .SetProperty(a => a.UpdatedAt, DateTime.UtcNow), cancellationToken);
            }
        }


        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TelegramChatImportService> _logger;
        private readonly TelegramAdPersistenceService _adDataService;
        private readonly TelegramChatPersistenceService _telegramChatPersistenceService;
        private readonly TelegramMessagePersistenceService _telegramMessagePersistenceService;
        private readonly UploadOptions _options;
        private readonly SimilarityCalculator _similarityCalculator;
    }
}