using TgJobAdAnalytics.Models.Vectors;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Computes MinHash signatures over deterministic shingles using the active model params.
/// </summary>
public sealed class MinHashVectorizer
{
    public MinHashVectorizer(OptionVectorizationConfig config)
    {
        _vectorizationConfig = config;
    }


    /// <summary>
    /// Generates a MinHash signature and shingle count for the specified text using the current vectorization
    /// configuration.
    /// </summary>
    /// <remarks>The method normalizes the input text and computes its MinHash signature based on the active
    /// vectorization parameters. The shingle count reflects the number of unique text fragments used in the signature
    /// calculation. This method is thread-safe.</remarks>
    /// <param name="text">The input text to be normalized and processed for signature generation. Cannot be null.</param>
    /// <returns>A tuple containing an array of unsigned integers representing the MinHash signature and an integer indicating
    /// the number of shingles generated from the input text.</returns>
    public (uint[] Signature, int ShingleCount) GenerateMinHashSignature(string text)
    {
        var p = _vectorizationConfig.GetActive();
        var normalized = TextNormalizer.NormalizeAdText(text);
        var shingles = GetShingles(normalized, p.ShingleSize);

        var calc = new Messages.MinHashCalculator(
            vectorizationOptions: new TgJobAdAnalytics.Models.Messages.VectorizationOptions
            {
                HashFunctionCount = p.HashFunctionCount,
                LshBandCount = p.LshBandCount,
                MinHashSeed = p.MinHashSeed,
                ShingleSize = p.ShingleSize,
                CurrentVersion = p.Version,
                NormalizationVersion = "default",
                VocabularySize = p.VocabularySize
            },
            vocabularySize: p.VocabularySize);

        var sig = calc.GenerateSignature(shingles).ToArray();
        return (sig, shingles.Count);
    }


    private static HashSet<string> GetShingles(string text, int size)
    {
        if (size <= 0 || text.Length < size)
            return [];

        var set = new HashSet<string>();
        for (int i = 0; i <= text.Length - size; i++)
            set.Add(text.Substring(i, size));

        return set;
    }


    private readonly OptionVectorizationConfig _vectorizationConfig;
}
