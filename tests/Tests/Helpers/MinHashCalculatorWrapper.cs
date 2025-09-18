using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Services.Messages;

namespace Tests.Helpers;

/// <summary>
/// Compatibility wrapper exposing a simple constructor used by tests while delegating to the actual MinHash implementation.
/// </summary>
public sealed class MinHashCalculatorWrapper
{
    /// <summary>
    /// Initializes a new instance of the MinHashCalculator with the given parameters.
    /// </summary>
    /// <param name="hashFunctionCount">Number of hash functions.</param>
    /// <param name="vocabularySize">Estimated vocabulary size.</param>
    public MinHashCalculatorWrapper(int hashFunctionCount, int vocabularySize)
        : this(hashFunctionCount, vocabularySize, seed: 1000)
    {
    }


    /// <summary>
    /// Initializes a new instance of the MinHashCalculator with an explicit seed.
    /// </summary>
    /// <param name="hashFunctionCount">Number of hash functions.</param>
    /// <param name="vocabularySize">Estimated vocabulary size.</param>
    /// <param name="seed">Seed used to generate hash functions.</param>
    public MinHashCalculatorWrapper(int hashFunctionCount, int vocabularySize, int seed)
    {
        var options = new VectorizationOptions
        {
            HashFunctionCount = hashFunctionCount,
            MinHashSeed = seed,
            LshBandCount = 1,
            ShingleSize = 1,
            CurrentVersion = 1,
            NormalizationVersion = "v1"
        };

        _inner = new MinHashCalculator(options, vocabularySize);
    }


    /// <summary>
    /// Gets the number of hash functions used for the signature.
    /// </summary>
    public int HashFunctionCount 
        => _inner.HashFunctionCount;

    /// <summary>
    /// Generates a MinHash signature for a given set of shingles.
    /// </summary>
    /// <param name="shingles">Set of shingles.</param>
    /// <returns>Signature as a span of uint values.</returns>
    public ReadOnlySpan<uint> GenerateSignature(HashSet<string> shingles) 
        => _inner.GenerateSignature(shingles);


    private readonly MinHashCalculator _inner;
}
