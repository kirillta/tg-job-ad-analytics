namespace TgJobAdAnalytics.Models.Messages;

/// <summary>
/// Provides configuration options for the text vectorization pipeline, including MinHash and LSH parameters
/// used to compute similarity between job advertisement messages. Changing these values (except runtime-only
/// ones) can invalidate previously computed signatures and may require recomputation and/or a model version bump.
/// </summary>
public class VectorizationOptions
{
    /// <summary>
    /// Gets or sets the logical version of this configuration. Increment when changes require regeneration of cached data.
    /// </summary>
    public int CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the number of hash functions used to build the MinHash signature. Higher values increase accuracy but cost more CPU.
    /// </summary>
    public int HashFunctionCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of bands used in Locality Sensitive Hashing (LSH) when splitting the MinHash signature.
    /// More bands reduce false positives but may increase false negatives.
    /// </summary>
    public int LshBandCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the starting seed used to deterministically generate the sequence of hash functions for MinHash.
    /// </summary>
    public int MinHashSeed { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the identifier of the normalization algorithm / ruleset (e.g., token filtering, case folding) applied before shingling.
    /// Changing this alters tokenization and therefore all downstream signatures.
    /// </summary>
    public string NormalizationVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token shingle (n-gram) size used when generating sets for MinHash.
    /// </summary>
    public int ShingleSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the fixed vocabulary (universe) size used to derive MinHash bit-width. Changing this requires a new model version.
    /// </summary>
    public int VocabularySize { get; set; } = 1_000_000;
}
