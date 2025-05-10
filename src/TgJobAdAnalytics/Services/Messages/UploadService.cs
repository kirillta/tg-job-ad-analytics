using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Messages
{
    public class UploadService
    {
        public UploadService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task UpdateFromJson(string sourcePath)
        {
            var existingChatTelegramIds = await _dbContext.Chats
                .Select(c => c.TelegramId)
                .ToHashSetAsync();

            var fileNames = Directory.GetFiles(sourcePath);
            foreach (string fileName in fileNames)
            {
                if (!fileName.EndsWith(".json"))
                    continue;

                Console.WriteLine($"Processing file: {Path.GetFileName(fileName)}");

                try
                {
                    using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                    var buffer = new byte[json.Length];
                    await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));
                    var chat = JsonSerializer.Deserialize<TgChat>(buffer);

                    var isChatExist = existingChatTelegramIds.Contains(chat.Id);
                    ChatEntity chatEntity;
                    if (isChatExist)
                        chatEntity = await ProcessExistingChat(chat);
                    else
                        chatEntity = ProcessNewChat(chat);
                    
                    await _dbContext.SaveChangesAsync();

                    if (isChatExist)
                        await ProcessExistingChatMessages(chat);
                    else
                        await ProcessNewChatMessages(chat);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
                }
            }

            Console.WriteLine("Chat processing completed");
        }


        private async ValueTask<ChatEntity> ProcessExistingChat(TgChat chat)
        {
            var existingChat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.TelegramId == chat.Id);

            ArgumentNullException.ThrowIfNull(existingChat, $"Chat with ID {chat.Id} not found in the database.");

            existingChat.Name = chat.Name;
            existingChat.UpdatedAt = DateTime.UtcNow;

            _dbContext.Chats.Update(existingChat);
            Console.WriteLine($"Updated existing chat: {chat.Name}");

            return existingChat;
        }


        private async Task ProcessExistingChatMessages(TgChat chat)
        {
            var existingMessageTelegramIds = await _dbContext.Messages
                .Where(m => m.TelegramChatId == chat.Id)
                .Select(m => m.TelegramMessageId)
                .ToHashSetAsync();

            var targetMessages = chat.Messages
                .Where(m => !existingMessageTelegramIds.Contains(m.Id))
                .ToList();

            await ProcessMessages(chat, targetMessages);
        }


        private async Task ProcessMessages(TgChat chat, List<TgMessage> messages)
        {
            var entries = new List<MessageEntity>(messages.Count);
            foreach (var tgMessage in messages)
            {
                var now = DateTime.UtcNow;
                var messageEntity = new MessageEntity
                {
                    TelegramChatId = chat.Id,
                    TelegramMessageId = tgMessage.Id,
                    TelegramMessageDate = tgMessage.Date,
                    TextEntries = ToRawEntries(tgMessage.TextEntities),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                entries.Add(messageEntity);
            }

            const int batchSize = 1000;
            var addedCount = 0;
            for (int i = 0; i < entries.Count; i += batchSize)
            {
                var batch = entries.Skip(i).Take(batchSize);
                await _dbContext.Messages.AddRangeAsync(batch);
                await _dbContext.SaveChangesAsync();

                addedCount += batch.Count();
            }
            
            Console.WriteLine($"Added {addedCount} messages to the database.");

            static List<KeyValuePair<TgTextEntryType, string>> ToRawEntries(List<TgTextEntry> entries) 
                => [.. entries.Select(e => new KeyValuePair<TgTextEntryType, string>(e.Type, e.Text))];
        }


        private ChatEntity ProcessNewChat(in TgChat chat)
        {
            var now = DateTime.UtcNow;

            var chatEntity = new ChatEntity
            {
                TelegramId = chat.Id,
                Name = chat.Name,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Chats.Add(chatEntity);
            Console.WriteLine($"Added new chat: {chat.Name}");

            return chatEntity;
        }


        private Task ProcessNewChatMessages(TgChat chat) 
            => ProcessMessages(chat, chat.Messages);


        private readonly ApplicationDbContext _dbContext;
    }
}