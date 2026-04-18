using TgJobAdAnalytics.Models.Locations.Enums;

namespace TgJobAdAnalytics.Data.Messages;

public class AdEntity
{
    public AdEntity()
    {
    }


    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public bool IsUnique { get; set; } = false;
    public string Text { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public Guid? StackId { get; set; }
    public VacancyLocation Location { get; set; } = VacancyLocation.Unknown;
    public WorkFormat WorkFormat { get; set; } = WorkFormat.Unknown;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
