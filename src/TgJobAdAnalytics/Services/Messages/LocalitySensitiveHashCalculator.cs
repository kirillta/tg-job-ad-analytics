using System.Collections.Concurrent;
using System.Diagnostics;
using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Services.Messages;

/// <summary>
/// Provides an in-memory Locality Sensitive Hashing (LSH) index over MinHash signatures to rapidly
/// identify near-duplicate / similar job advertisement messages. Signatures are partitioned into bands
/// and stored in per-band hash tables for candidate generation.
/// </summary>
public sealed class LocalitySensitiveHashCalculator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalitySensitiveHashCalculator"/>.
    /// Validates band / hash function configuration and allocates per-band hash tables.
    /// </summary>
    /// <param name="vectorizationOptions">Vectorization options supplying LSH band and hash function counts.</param>
    public LocalitySensitiveHashCalculator(VectorizationOptions vectorizationOptions)
    {
        Debug.Assert(vectorizationOptions.HashFunctionCount > 0, "hashTableCount must be greater than 0");
        Debug.Assert(vectorizationOptions.LshBandCount > 0, "bandCount must be greater than 0");
        Debug.Assert(vectorizationOptions.HashFunctionCount % vectorizationOptions.LshBandCount == 0, "hashTableCount must be divisible by bandCount");

        _bandCount = vectorizationOptions.LshBandCount;
        _rowCount = vectorizationOptions.HashFunctionCount / vectorizationOptions.LshBandCount;

        _storedMessageIds = new ConcurrentDictionary<Guid, bool>();

        _hashTables = new List<ConcurrentDictionary<uint, ConcurrentBag<Guid>>>(_bandCount);
        for (int i = 0; i < _bandCount; i++)
            _hashTables.Add(new ConcurrentDictionary<uint, ConcurrentBag<Guid>>());
    }


    /// <summary>
    /// Adds a MinHash signature to the LSH index. Each band hash bucket stores the item id for candidate retrieval.
    /// </summary>
    /// <param name="itemId">Unique identifier of the message / item represented by the signature.</param>
    /// <param name="signature">Complete MinHash signature (array of hash values) partitioned into bands.</param>
    /// <exception cref="Exception">Thrown if the provided <paramref name="itemId"/> has already been added.</exception>
    public void Add(Guid itemId, ReadOnlySpan<uint> signature)
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


    /// <summary>
    /// Returns candidate matching item identifiers that share at least one identical band hash with the provided signature.
    /// </summary>
    /// <param name="signature">MinHash signature to query.</param>
    /// <returns>List of candidate item ids (may contain false positives, but not the query item unless previously added).</returns>
    public List<Guid> GetMatches(ReadOnlySpan<uint> signature)
    {
        if (signature.Length == 0)
            return [];

        var signatureLength = signature.Length;

        var candidates = new HashSet<Guid>();
        for (var band = 0; band < _bandCount; band++)
        {
            var bandHash = ComputeBandHash(signature, band, _rowCount, signatureLength);
            if (_hashTables[band].TryGetValue(bandHash, out ConcurrentBag<Guid>? value))
                candidates.UnionWith(value);
        }

        return [.. candidates];
    }


    /// <summary>
    /// Gets the number of LSH bands (hash tables).
    /// </summary>
    public int BandCount 
        => _bandCount;


    /// <summary>
    /// Gets the number of rows (hashes) per band.
    /// </summary>
    public int RowCount 
        => _rowCount;


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
    private readonly List<ConcurrentDictionary<uint, ConcurrentBag<Guid>>> _hashTables;
    private readonly ConcurrentDictionary<Guid, bool> _storedMessageIds;
}
