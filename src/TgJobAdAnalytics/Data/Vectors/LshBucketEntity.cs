using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

public class LshBucketEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public int Version { get; set; }

    public int Band { get; set; }

    [Required]
    public string Key { get; set; } = string.Empty;

    public Guid AdId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
