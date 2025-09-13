namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Computes master-vectors (MinHash signatures) for ad texts.
/// </summary>
public interface IMinHashVectorizer
{
    (uint[] Signature, int ShingleCount) Compute(string text);
}
