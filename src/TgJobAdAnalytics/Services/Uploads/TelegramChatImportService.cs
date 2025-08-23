using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Uploads
{
    public class TelegramChatImportService
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


        public async Task ImportFromJson(string sourcePath)
        {
            if (_options.Mode == UploadMode.Skip)
            {
                _logger.LogInformation("Update skipped due to configuration settings");
                return;
            }

            if (_options.Mode == UploadMode.Clean)
            {
                await _telegramChatPersistenceService.RemoveAll();
                await _telegramMessagePersistenceService.RemoveAll();
                await _adDataService.RemoveAll();
            }

            var timeStamp = DateTime.UtcNow;
            var fileNames = Directory.GetFiles(sourcePath);
            foreach (string fileName in fileNames)
            {
                if (!fileName.EndsWith(".json"))
                    continue;

                var chatProcessingTime = Stopwatch.GetTimestamp();
                var chatFileName = Path.GetFileName(fileName);
                _logger.LogInformation("Processing file: {FileName}", chatFileName);

                var chat = await ReadChatFromFile(fileName);

                var chatState = await _telegramChatPersistenceService.DetermineState(chat);
                await _telegramChatPersistenceService.Upsert(chat, chatState, timeStamp);
                await _telegramMessagePersistenceService.Upsert(chat, chatState, timeStamp);
                await _adDataService.Upsert(chat, chatState, timeStamp);

                _logger.LogInformation("File {FileName} processed in {ElapsedSeconds} seconds", chatFileName, Stopwatch.GetElapsedTime(chatProcessingTime).TotalSeconds);
            }            
        
            _logger.LogInformation("Chat processing completed");
            await DeduplicateAds();


            static async Task<TgChat> ReadChatFromFile(string name)
            {
                using var json = new FileStream(name, FileMode.Open, FileAccess.Read);
                var buffer = new byte[json.Length];
                await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));

                return JsonSerializer.Deserialize<TgChat>(buffer);
            }
        }


        private async Task DeduplicateAds()
        {
            var ads = await _dbContext.Ads
                .AsNoTracking()
                .IgnoreQueryFilters()
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var uniqueAds = _similarityCalculator.Distinct(ads);
            _logger.LogInformation("Found {UniqueAdCount} unique ads out of {TotalAdCount}", uniqueAds.Count, ads.Count);

            foreach (var batch in uniqueAds.Chunk(_options.BatchSize))
            {
                await _dbContext.Ads
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(ad => batch.Select(b => b.Id).Contains(ad.Id))
                    .ExecuteUpdateAsync(b => b
                    .SetProperty(a => a.IsUnique, true)
                    .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
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