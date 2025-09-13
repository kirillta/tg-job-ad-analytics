using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

/// <summary>
/// Stores the active parameters and thresholds for the vectorization model used to compute master-vectors.
/// </summary>
public class VectorModelVersionEntity
{
    /// <summary>
    /// Primary key and model version identifier. Bump when parameters change.
    /// </summary>
    [Key]
    public int Version { get; set; }

    /// <summary>
    /// Text normalization version identifier to ensure deterministic processing.
    /// </summary>
    [Required]
    public string NormalizationVersion { get; set; } = string.Empty;

    /// <summary>
    /// Shingle size used to construct the set of tokens for MinHash.
    /// </summary>
    public int ShingleSize { get; set; }

    /// <summary>
    /// Number of hash functions in the MinHash signature.
    /// </summary>
    public int HashFunctionCount { get; set; }

    /// <summary>
    /// Seed used to generate deterministic hash functions.
    /// </summary>
    public int MinHashSeed { get; set; }

    /// <summary>
    /// Number of bands used by LSH.
    /// </summary>
    public int LshBandCount { get; set; }

    /// <summary>
    /// Number of rows per band used by LSH.
    /// </summary>
    public int LshRowsPerBand { get; set; }

    /// <summary>
    /// Fixed universe size backing MinHash bit-width.
    /// </summary>
    public int VocabularySize { get; set; }

    /// <summary>
    /// Threshold for duplicate detection using MinHash/Jaccard.
    /// </summary>
    public double DuplicateThreshold { get; set; }

    /// <summary>
    /// Threshold for similar ads retrieval using MinHash/Jaccard.
    /// </summary>
    public double SimilarThreshold { get; set; }

    /// <summary>
    /// Indicates whether this model version is the active one.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
