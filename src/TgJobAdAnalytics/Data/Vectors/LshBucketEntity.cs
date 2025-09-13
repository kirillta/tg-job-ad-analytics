using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

/// <summary>
/// LSH band buckets pointing from a band key to a set of ads that share this key for a specific model version.
/// </summary>
public class LshBucketEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Model version used to compute LSH keys.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Band index (0-based).
    /// </summary>
    public int Band { get; set; }

    /// <summary>
    /// Hash key representing this band for a signature.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Associated ad identifier.
    /// </summary>
    public Guid AdId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
