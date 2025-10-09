using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads.Enums;

namespace TgJobAdAnalytics.Services.Uploads;

/// <summary>
/// Persists Telegram chat metadata and determines ingestion state (new vs existing) to drive upload workflows.
/// Provides idempotent upsert semantics and bulk removal support.
/// </summary>
public sealed class TelegramChatPersistenceService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramChatPersistenceService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="dbContext">Application database context.</param>
    public TelegramChatPersistenceService(ILogger<TelegramChatPersistenceService> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    /// <summary>
    /// Deletes all chat records and commits the change.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning all chat data...");
        await _dbContext.Chats.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All chat data has been removed");
    }


    /// <summary>
    /// Determines whether the provided chat should be treated as new or existing by checking for prior chat or message rows.
    /// </summary>
    /// <param name="chat">Telegram chat payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="UploadedDataState.New"/> when neither chat nor messages exist; otherwise <see cref="UploadedDataState.Existing"/>.</returns>
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


    /// <summary>
    /// Inserts a new chat or updates an existing one based on the supplied state.
    /// </summary>
    /// <param name="chat">Telegram chat payload.</param>
    /// <param name="state">Previously determined upload state.</param>
    /// <param name="timestamp">Timestamp applied to audit fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TelegramChatPersistenceService> _logger;
}
