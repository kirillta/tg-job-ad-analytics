using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Models.Vectors;

/// <summary>
/// Parameters defining the active vectorization model used to compute master-vectors and LSH keys.
/// </summary>
public sealed class VectorizationModelParams
{
    public int Version { get; init; }

    public int ShingleSize { get; init; }

    public int HashFunctionCount { get; init; }

    public int MinHashSeed { get; init; }

    public int LshBandCount { get; init; }

    public int RowsPerBand => HashFunctionCount / LshBandCount;

    /// <summary>
    /// Fixed vocabulary size used to derive universeBitSize for MinHash function.
    /// Keep constant per model version for determinism.
    /// </summary>
    public int VocabularySize { get; init; } = 1_000_000;

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
}
