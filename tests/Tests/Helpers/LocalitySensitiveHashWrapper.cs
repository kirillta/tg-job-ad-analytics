using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Services.Messages;

namespace Tests.Helpers;

/// <summary>
/// Compatibility wrapper exposing simple ctor and signature overloads used by tests, delegating to the actual LSH implementation.
/// </summary>
public sealed class LocalitySensitiveHashWrapper
{
    /// <summary>
    /// Initializes a new instance with given signature length and band count.
    /// </summary>
    /// <param name="hashFunctionCount">Signature length (rows).</param>
    /// <param name="bandCount">Number of bands.</param>
    public LocalitySensitiveHashWrapper(int hashFunctionCount, int bandCount)
    {
        var options = new VectorizationOptions
        {
            HashFunctionCount = hashFunctionCount,
            LshBandCount = bandCount,
            MinHashSeed = 1000,
            ShingleSize = 1,
            CurrentVersion = 1,
            NormalizationVersion = "v1"
        };

        _inner = new LocalitySensitiveHashCalculator(options);
    }


    /// <summary>
    /// Add an item by numeric id.
    /// </summary>
    public void Add(long itemId, ReadOnlySpan<uint> signature) 
        => _inner.Add(IdToGuid(itemId), signature);


    /// <summary>
    /// Add an item by numeric id and array.
    /// </summary>
    public void Add(long itemId, uint[] signature) 
        => _inner.Add(IdToGuid(itemId), signature);


    /// <summary>
    /// Returns candidate ids for a given signature.
    /// </summary>
    public List<long> GetMatches(ReadOnlySpan<uint> signature)
    {
        var matches = _inner.GetMatches(signature);
        var result = new List<long>(matches.Count);
        foreach (var match in matches)
            result.Add(GuidToId(match));

        return result;
    }


    private static Guid IdToGuid(long id)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, id);

        return new Guid(bytes);
    }


    private static long GuidToId(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return BitConverter.ToInt64(bytes, 0);
    }


    private readonly LocalitySensitiveHashCalculator _inner;
}
