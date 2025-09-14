using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads.Enums;

namespace TgJobAdAnalytics.Services.Uploads;

public class TelegramChatPersistenceService
{
    public TelegramChatPersistenceService(ILogger<TelegramChatPersistenceService> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    public async Task RemoveAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning all chat data...");
        await _dbContext.Chats.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All chat data has been removed");
    }


    public async Task<UploadedDataState> DetermineState(TgChat chat, CancellationToken cancellationToken)
    {
        var hasChat = await _dbContext.Chats
            .AnyAsync(c => c.TelegramId == chat.Id, cancellationToken);

        if (hasChat)
            return UploadedDataState.Existing;

        var hasMessages = await _dbContext.Messages
            .AnyAsync(m => m.TelegramChatId == chat.Id, cancellationToken);

        return hasMessages 
            ? UploadedDataState.Existing 
            : UploadedDataState.New;
    }


    public async ValueTask Upsert(TgChat chat, UploadedDataState state, DateTime timestamp, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UploadedDataState.New:
                Add(in chat, timestamp);
                break;
            case UploadedDataState.Existing:
                await Update(chat, timestamp, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    private void Add(in TgChat chat, in DateTime timeStamp)
    {
        var chatEntity = new ChatEntity
        {
            TelegramId = chat.Id,
            Name = chat.Name,
            CreatedAt = timeStamp,
            UpdatedAt = timeStamp
        };

        _dbContext.Chats.Add(chatEntity);
        _logger.LogInformation("Added new chat: {ChatName}", chat.Name);
    }


    private async ValueTask Update(TgChat chat, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var existingChat = await _dbContext.Chats
            .FirstOrDefaultAsync(c => c.TelegramId == chat.Id, cancellationToken);

        if (existingChat is null)
        {
            Add(in chat, timeStamp);
            return;
        }

        existingChat.Name = chat.Name;
        existingChat.UpdatedAt = timeStamp;

        _dbContext.Chats.Update(existingChat);
        _logger.LogInformation("Updated existing chat: {ChatName}", chat.Name);
    }


    private readonly ILogger<TelegramChatPersistenceService> _logger;
    private readonly ApplicationDbContext _dbContext;
}
