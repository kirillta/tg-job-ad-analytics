using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Data.Messages;

public class MessageEntity
{
    public MessageEntity()
    {
    }


    public Guid Id { get; set; }

    public long TelegramChatId { get; set; }

    public long TelegramMessageId { get; set; }

    public DateTime TelegramMessageDate { get; set; }

    public List<KeyValuePair<TgTextEntryType, string>> TextEntries { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
