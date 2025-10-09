using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Models.Vectors;

/// <summary>
/// Parameters defining the active vectorization model used to compute master vectors, MinHash signatures and LSH keys.
/// Immutable once created; derive via <see cref="FromOptions(VectorizationOptions)"/>.
/// </summary>
public sealed class VectorizationModelParams
{
    /// <summary>
    /// Creates <see cref="VectorizationModelParams"/> from mutable <see cref="VectorizationOptions"/> ensuring sane defaults.
    /// </summary>
    /// <param name="options">Source options provided by configuration or user input.</param>
    /// <returns>Immutable parameter set representing the active vectorization model.</returns>
    public static VectorizationModelParams FromOptions(VectorizationOptions options)
        => new()
        {
            Version = options.CurrentVersion <= 0 ? 1 : options.CurrentVersion,
            ShingleSize = options.ShingleSize,
            HashFunctionCount = options.HashFunctionCount,
            MinHashSeed = options.MinHashSeed,
            LshBandCount = options.LshBandCount,
            VocabularySize = options.VocabularySize
        };


    /// <summary>
    /// Gets the logical version of the vectorization model. Increment when a breaking change to the signature format occurs.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Gets the token shingle (n-gram) size used when constructing the universe for MinHash.
    /// </summary>
    public int ShingleSize { get; init; }

    /// <summary>
    /// Gets the number of hash functions used to construct the MinHash signature. Higher counts increase accuracy at higher CPU cost.
    /// </summary>
    public int HashFunctionCount { get; init; }

    /// <summary>
    /// Gets the starting seed used for deterministic generation of MinHash hash functions.
    /// </summary>
    public int MinHashSeed { get; init; }

    /// <summary>
    /// Gets the number of bands into which the MinHash signature is partitioned for LSH bucketing.
    /// </summary>
    public int LshBandCount { get; init; }

    /// <summary>
    /// Gets the number of rows (hash values) per band in the MinHash signature. Equal to <c>HashFunctionCount / LshBandCount</c>.
    /// </summary>
    public int RowsPerBand 
        => HashFunctionCount / LshBandCount;

    /// <summary>
    /// Fixed vocabulary size used to derive universeBitSize for the MinHash function.
    /// Keep constant per model version for determinism.
    /// </summary>
    public int VocabularySize { get; init; } = 1_000_000;
}
