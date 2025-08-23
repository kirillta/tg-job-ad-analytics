namespace TgJobAdAnalytics.Data.Messages;

public class ChatEntity
{
    public ChatEntity()
    {
    }


    public Guid Id { get; set; } = Guid.CreateVersion7();

    public long TelegramId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
