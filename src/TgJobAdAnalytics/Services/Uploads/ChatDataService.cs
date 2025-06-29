using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads.Enums;

namespace TgJobAdAnalytics.Services.Uploads;

public class ChatDataService
{
    public ChatDataService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<UploadedDataState> GetChatState(TgChat chat)
    {
        var existingChat = await _dbContext.Chats
            .AnyAsync(c => c.TelegramId == chat.Id);

        if (existingChat)
            return UploadedDataState.Existing;

        return UploadedDataState.New;
    }


    public async ValueTask Update(TgChat chat, UploadedDataState state, DateTime timestamp)
    {
        switch (state)
        {
            case UploadedDataState.New:
                AddNew(in chat, timestamp);
                break;
            case UploadedDataState.Existing:
                await UpdateExisting(chat, timestamp);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        await _dbContext.SaveChangesAsync();
    }


    private void AddNew(in TgChat chat, in DateTime timeStamp)
    {
        var chatEntity = new ChatEntity
        {
            TelegramId = chat.Id,
            Name = chat.Name,
            CreatedAt = timeStamp,
            UpdatedAt = timeStamp
        };

        _dbContext.Chats.Add(chatEntity);
        Console.WriteLine($"Added new chat: {chat.Name}");
    }


    private async ValueTask UpdateExisting(TgChat chat, DateTime timeStamp)
    {
        var existingChat = await _dbContext.Chats
            .FirstOrDefaultAsync(c => c.TelegramId == chat.Id);

        ArgumentNullException.ThrowIfNull(existingChat, $"Chat with ID {chat.Id} not found in the database.");

        existingChat.Name = chat.Name;
        existingChat.UpdatedAt = timeStamp;

        _dbContext.Chats.Update(existingChat);
        Console.WriteLine($"Updated existing chat: {chat.Name}");
    }


    private readonly ApplicationDbContext _dbContext;
}
