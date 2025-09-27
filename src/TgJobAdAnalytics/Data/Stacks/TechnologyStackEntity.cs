using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Stacks;

public class TechnologyStackEntity
{
    public TechnologyStackEntity()
    {
    }


    public Guid Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
