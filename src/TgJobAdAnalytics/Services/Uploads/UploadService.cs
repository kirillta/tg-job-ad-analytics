using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;

namespace TgJobAdAnalytics.Services.Uploads
{
    /// <summary>
    /// Service responsible for uploading and updating Telegram chat messages from JSON files.
    /// </summary>
    public class UploadService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <param name="options">The upload options.</param>
        /// <param name="chatDataService">The chat data service.</param>
        /// <param name="messageDataService">The message data service.</param>
        public UploadService(
            IOptions<UploadOptions> options, 
            AdDataService adDataService,
            ChatDataService chatDataService, 
            MessageDataService messageDataService)
        {
            _adDataService = adDataService;
            _chatDataService = chatDataService;
            _messageDataService = messageDataService;
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
            {
                await _chatDataService.CleanData();
                await _messageDataService.CleanData();
                await _adDataService.CleanData();
            }

            var timeStamp = DateTime.UtcNow;
            var fileNames = Directory.GetFiles(sourcePath);
            foreach (string fileName in fileNames)
            {
                if (!fileName.EndsWith(".json"))
                    continue;

                Console.WriteLine($"Processing file: {Path.GetFileName(fileName)}");

                var chat = await GetChat(fileName);

                var chatState = await _chatDataService.GetChatState(chat);
                await _chatDataService.Update(chat, chatState, timeStamp);
                await _messageDataService.Update(chat, chatState, timeStamp);
                await _adDataService.Update(chat, chatState, timeStamp);
            }            
        
            Console.WriteLine("Chat processing completed");
            return;


            static async Task<TgChat> GetChat(string name)
            {
                using var json = new FileStream(name, FileMode.Open, FileAccess.Read);
                var buffer = new byte[json.Length];
                await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));

                return JsonSerializer.Deserialize<TgChat>(buffer);
            }
        }

        
        private readonly AdDataService _adDataService;
        private readonly ChatDataService _chatDataService;
        private readonly MessageDataService _messageDataService;
        private readonly UploadOptions _options;
    }
}