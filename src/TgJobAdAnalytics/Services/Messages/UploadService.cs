using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Messages
{
    /// <summary>
    /// Service responsible for uploading and updating Telegram chat messages from JSON files.
    /// </summary>
    public class UploadService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="options">The upload options.</param>
        public UploadService(ApplicationDbContext dbContext, IOptions<UploadOptions> options)
        {
            _dbContext = dbContext;
            _options = options.Value;
        }


        /// <summary>
        /// Updates the database with chat and message data from JSON files in the specified directory.
        /// </summary>
        /// <param name="sourcePath">The directory path containing JSON files to process.</param>
        public async Task UpdateFromJson(string sourcePath)
        {
            if (_options.Mode == UploadMode.Skip)
            {
                Console.WriteLine("Update skipped due to configuration settings.");
                return;
            }

            if (_options.Mode == UploadMode.Clean)
                await CleanMessageData();

            var existingChatTelegramIds = await _dbContext.Chats
                .Select(c => c.TelegramId)
                .ToHashSetAsync();
            
            var timeStamp = DateTime.UtcNow;
            var fileNames = Directory.GetFiles(sourcePath);
            foreach (string fileName in fileNames)
            {
                if (!fileName.EndsWith(".json"))
                    continue;

                Console.WriteLine($"Processing file: {Path.GetFileName(fileName)}");

                using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                var buffer = new byte[json.Length];
                await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));
                var chat = JsonSerializer.Deserialize<TgChat>(buffer);

                var isChatExist = existingChatTelegramIds.Contains(chat.Id);                    
                if (isChatExist)
                    await ProcessExistingChat(chat, timeStamp);
                else
                    ProcessNewChat(chat, timeStamp);
                    
                await _dbContext.SaveChangesAsync();                    
                    
                if (isChatExist)
                    await ProcessExistingChatMessages(chat, timeStamp);
                else
                    await ProcessNewChatMessages(chat, timeStamp);
            }            
        
            Console.WriteLine("Chat processing completed");
        }


        private async ValueTask ProcessExistingChat(TgChat chat, DateTime timeStamp)
        {
            var existingChat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.TelegramId == chat.Id);

            ArgumentNullException.ThrowIfNull(existingChat, $"Chat with ID {chat.Id} not found in the database.");

            existingChat.Name = chat.Name;
            existingChat.UpdatedAt = timeStamp;

            _dbContext.Chats.Update(existingChat);
            Console.WriteLine($"Updated existing chat: {chat.Name}");
        }


        private async Task ProcessExistingChatMessages(TgChat chat, DateTime timeStamp)
        {
            var existingMessageTelegramIds = await _dbContext.Messages
                .Where(m => m.TelegramChatId == chat.Id)
                .Select(m => m.TelegramMessageId)
                .ToHashSetAsync();

            var targetMessages = chat.Messages
                .Where(m => !existingMessageTelegramIds.Contains(m.Id))
                .ToList();            
            
            await ProcessMessages(chat, targetMessages, timeStamp);
        }


        private async Task ProcessMessages(TgChat chat, List<TgMessage> messages, DateTime timeStamp)
        {
            var entries = new List<MessageEntity>(messages.Count);
            foreach (var tgMessage in messages)
            {
                var messageEntity = new MessageEntity
                {
                    TelegramChatId = chat.Id,
                    TelegramMessageId = tgMessage.Id,
                    TelegramMessageDate = tgMessage.Date,
                    TextEntries = ToRawEntries(tgMessage.TextEntities),
                    Tags = ToRawTags(tgMessage.TextEntities),
                    CreatedAt = timeStamp,
                    UpdatedAt = timeStamp
                };                
                
                entries.Add(messageEntity);
            }

            var batchSize = _options.BatchSize;
            var addedCount = 0;
            for (int i = 0; i < entries.Count; i += batchSize)
            {
                int currentBatchSize = Math.Min(batchSize, entries.Count - i);
                var batch = entries.GetRange(i, currentBatchSize);
        
                await _dbContext.Messages.AddRangeAsync(batch);
                await _dbContext.SaveChangesAsync();

                addedCount += currentBatchSize;
            }
            
            Console.WriteLine($"Added {addedCount} messages to the database.");


            static List<string> ToRawTags(List<TgTextEntry> entries)
            {
                var results = new List<string>(entries.Count);
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.Text))
                        continue;

                    if (entry.Type == TgTextEntryType.HashTag)
                        results.Add(entry.Text.ToLowerInvariant());
                }

                return results;
            }


            static List<KeyValuePair<TgTextEntryType, string>> ToRawEntries(List<TgTextEntry> entries)
            {
                var results = new List<KeyValuePair<TgTextEntryType, string>>(entries.Count);
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.Text))
                        continue;

                    results.Add(new KeyValuePair<TgTextEntryType, string>(entry.Type, entry.Text));
                }

                return results;
            }
        }


        private void ProcessNewChat(in TgChat chat, in DateTime timeStamp)
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


        private Task ProcessNewChatMessages(TgChat chat, DateTime timeStamp) 
            => ProcessMessages(chat, chat.Messages, timeStamp);


        private async Task CleanMessageData()
        {
            Console.WriteLine("Cleaning all message data...");
            await _dbContext.Messages.ExecuteDeleteAsync();
            Console.WriteLine("All message data has been removed.");
        }


        private readonly ApplicationDbContext _dbContext;
        private readonly UploadOptions _options;
    }
}