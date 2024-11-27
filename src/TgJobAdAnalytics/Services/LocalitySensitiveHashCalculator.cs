using System.Collections.Concurrent;
using System.Diagnostics;

namespace TgJobAdAnalytics.Services;

public sealed class LocalitySensitiveHashCalculator
{
    public LocalitySensitiveHashCalculator(int hashTableCount, int bandCount = 20)
    {
        Debug.Assert(hashTableCount > 0, "hashTableCount must be greater than 0");
        Debug.Assert(bandCount > 0, "bandCount must be greater than 0");
        Debug.Assert(hashTableCount % bandCount == 0, "hashTableCount must be divisible by bandCount");

        _bandCount = bandCount;
        _rowCount = hashTableCount / bandCount;

        _storedMessageIds = new ConcurrentDictionary<long, bool>();

        _hashTables = new List<ConcurrentDictionary<uint, ConcurrentBag<long>>>(_bandCount);
        for (int i = 0; i < _bandCount; i++)
            _hashTables.Add(new ConcurrentDictionary<uint, ConcurrentBag<long>>());
    }


    public void Add(long itemId, ReadOnlySpan<uint> signature)
    {
        if (!_storedMessageIds.TryAdd(itemId, true))
            throw new Exception($"Item with ID {itemId} is already stored");

        var signatureLength = signature.Length;
        for (var band = 0; band < _bandCount; band++)
        {
            var bandHash = ComputeBandHash(signature, band, _rowCount, signatureLength);
            _hashTables[band].GetOrAdd(bandHash, _ => []).Add(itemId);
        }
    }


    public List<long> GetMatches(ReadOnlySpan<uint> signature)
    {
        if (signature.Length == 0)
            return [];

        var signatureLength = signature.Length;

        var candidates = new HashSet<long>();
        for (var band = 0; band < _bandCount; band++)
        {
            var bandHash = ComputeBandHash(signature, band, _rowCount, signatureLength);
            if (_hashTables[band].TryGetValue(bandHash, out ConcurrentBag<long>? value))
                candidates.UnionWith(value);
        }

        return [.. candidates];
    }


    private static uint ComputeBandHash(ReadOnlySpan<uint> signature, int bandIndex, int rowsCount, int signatureLength)
    {
        var startIdx = bandIndex * rowsCount;
        var endIdx = Math.Min(startIdx + rowsCount, signatureLength);

        uint hash = 2166136261; // FNV offset basis
        for (var i = startIdx; i < endIdx; i++)
        {
            hash ^= signature[i];
            hash *= 16777619; // FNV prime
        }

        return hash;
    }


    private readonly int _bandCount;
    private readonly int _rowCount;
    private readonly List<ConcurrentDictionary<uint, ConcurrentBag<long>>> _hashTables;
    private readonly ConcurrentDictionary<long, bool> _storedMessageIds;
}
