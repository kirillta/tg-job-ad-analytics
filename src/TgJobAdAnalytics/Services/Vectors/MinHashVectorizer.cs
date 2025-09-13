using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Computes MinHash signatures over deterministic shingles using the active model params.
/// </summary>
public sealed class MinHashVectorizer : IMinHashVectorizer
{
    public MinHashVectorizer(IVectorizationConfig config)
    {
        _config = config;
    }

    public (uint[] Signature, int ShingleCount) Compute(string text)
    {
        var p = _config.GetActive();
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

    private readonly IVectorizationConfig _config;
}
