using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

public class AdVectorEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AdId { get; set; }

    public int Version { get; set; }

    public int Dim { get; set; }

    [Required]
    public byte[] Signature { get; set; } = [];

    [MaxLength(64)]
    public string? SignatureHash { get; set; }

    public int ShingleCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
