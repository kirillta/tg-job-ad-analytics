using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Uploads;

public class AdDataService
{
    public AdDataService(ApplicationDbContext dbContext, IOptions<ParallelOptions> parallelOptions, IOptions<UploadOptions> uploadOptions)
    {
        _dbContext = dbContext;
        _parallelOptions = parallelOptions.Value;
        _uploadOptions = uploadOptions.Value;
    }


    public async Task CleanData()
    {
        Console.WriteLine("Cleaning all ad data...");
        await _dbContext.Ads.ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("All ad data has been removed.");
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
        }
        ;
    }


    private async Task Add(TgChat chat, DateTime timeStamp)
    {
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .ToListAsync();

        await ProcessInternal(messages, timeStamp);
    }


    private async Task AddOnlyNew(TgChat chat, DateTime timeStamp)
    {
        var existingMessageIds = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id)
            .Select(m => m.Id)
            .ToHashSetAsync();

        var existingAdMessageIds = await _dbContext.Ads
            .Where(a => existingMessageIds.Contains(a.MessageId))
            .Select(a => a.MessageId)
            .ToHashSetAsync();

        var diff = existingMessageIds.Except(existingAdMessageIds);
        var messages = await _dbContext.Messages
            .Where(m => m.TelegramChatId == chat.Id && diff.Contains(m.Id))
            .ToListAsync();

        await ProcessInternal(messages, timeStamp);
    }


    private async Task ProcessInternal(List<MessageEntity> messages, DateTime timeStamp)
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

            addedCount += currentBatchSize;
        }

        Console.WriteLine($"Added {addedCount} ads to the database.");


        string GetText(MessageEntity message)
        {
            var stringBuilder = new StringBuilder();
            foreach (var entity in message.TextEntries)
            {
                if (entity.Key is TgTextEntryType.PlainText)
                    stringBuilder.Append(entity.Value);
            }

            if (stringBuilder.Length < MinimalValuebleMessageLength)
                return string.Empty;

            return TextNormalizer.Normalize(stringBuilder.ToString());
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

            if (!IsCurrentMonth())
                return false;

            return true;


            bool IsAd() 
                => hashTags.Any(AdTags.Contains);


            bool IsCurrentMonth()
                => message.TelegramMessageDate.Year == timeStamp.Year
                    && message.TelegramMessageDate.Month == timeStamp.Month;


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

    private readonly ApplicationDbContext _dbContext;
    private readonly ParallelOptions _parallelOptions;
    private readonly UploadOptions _uploadOptions;
}
