using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

/// <summary>
/// Persisted master-vector (MinHash signature) for an ad and a specific model version.
/// </summary>
public class AdVectorEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Associated advertisement identifier.
    /// </summary>
    public Guid AdId { get; set; }

    /// <summary>
    /// Model version used to compute the signature.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Signature dimension (should equal HashFunctionCount of the model version).
    /// </summary>
    public int Dim { get; set; }

    /// <summary>
    /// Serialized signature as a byte array (little-endian uint32 sequence).
    /// </summary>
    [Required]
    public byte[] Signature { get; set; } = [];

    /// <summary>
    /// Optional integrity hash (SHA256) of the signature for auditing.
    /// </summary>
    [MaxLength(64)]
    public string? SignatureHash { get; set; }

    /// <summary>
    /// Optional diagnostics: count of shingles used to build the signature.
    /// </summary>
    public int ShingleCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
