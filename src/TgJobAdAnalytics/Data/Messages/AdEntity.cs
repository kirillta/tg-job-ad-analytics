namespace TgJobAdAnalytics.Data.Messages;

/// <summary>
/// Represents an advertisement entity extracted from messages.
/// </summary>
public class AdEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdEntity"/> class.
    /// </summary>
    public AdEntity()
    {
    }


    /// <summary>
    /// Gets or sets the unique identifier for the advertisement.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the date when the advertisement was posted.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the text content of the advertisement.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the original message from which this ad was extracted.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the creation date of this entity.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update date of this entity.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
