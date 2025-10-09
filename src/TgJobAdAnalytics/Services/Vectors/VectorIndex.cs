using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;
using TgJobAdAnalytics.Models.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// EF-based LSH index (banded hashing) over signatures.
/// </summary>
public sealed class VectorIndex
{
    public VectorIndex(ApplicationDbContext dbContext, OptionVectorizationConfig vectorizationConfig)
    {
        _activeConfig = vectorizationConfig.GetActive();
        _dbContext = dbContext;
    }


    /// <summary>
    /// Inserts a new or updates an existing LSH bucket entry for the specified advertisement using the provided
    /// signature and timestamp.
    /// </summary>
    /// <remarks>This method processes each LSH band in the current configuration and upserts the
    /// corresponding bucket entry in the database. If an entry for the specified advertisement and band already exists,
    /// it will be updated; otherwise, a new entry will be created. The operation is performed asynchronously and can be
    /// cancelled via the provided cancellation token.</remarks>
    /// <param name="adId">The unique identifier of the advertisement to upsert.</param>
    /// <param name="signature">An array of unsigned integers representing the LSH signature bands for the advertisement. Must not be null.</param>
    /// <param name="timeStamp">The timestamp associated with the advertisement entry.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous upsert operation.</returns>
    public async Task Upsert(Guid adId, uint[] signature, DateTime timeStamp, CancellationToken cancellationToken)
    {
        for (var band = 0; band < _activeConfig.LshBandCount; band++)
        {
            var entity = ComputeBand(signature, band, in adId, in timeStamp);
            _dbContext.LshBuckets.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    /// <summary>
    /// Adds or updates a batch of LSH bucket entities in the database context for the specified advertisements and
    /// signatures, without saving changes to the database.
    /// </summary>
    /// <remarks>This method stages the upserted entities in the database context but does not commit the
    /// changes. To persist the changes, call the appropriate save method on the database context after invoking this
    /// method. If the list of items is empty, no entities are added.</remarks>
    /// <param name="items">A read-only list of tuples containing the advertisement identifier and its associated signature array. Each
    /// tuple represents an item to be upserted.</param>
    /// <param name="timeStamp">The timestamp to associate with each upserted entity.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous upsert operation.</returns>
    public async Task UpsertBatchWithoutSave(IReadOnlyList<(Guid AdId, uint[] Signature)> items, DateTime timeStamp, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var entities = new List<LshBucketEntity>(items.Count * _activeConfig.LshBandCount);

        foreach (var (adId, signature) in items)
        {
            for (var band = 0; band < _activeConfig.LshBandCount; band++)
            {
                var entity = ComputeBand(signature, band, in adId, in timeStamp);
                entities.Add(entity);
            }
        }

        await _dbContext.LshBuckets.AddRangeAsync(entities, cancellationToken);
    }


    /// <summary>
    /// Retrieves a collection of advertisement identifiers that match the specified signature using locality-sensitive
    /// hashing (LSH).
    /// </summary>
    /// <remarks>This method performs an LSH-based similarity search across all configured bands. The
    /// operation is asynchronous and may involve multiple database queries. The returned identifiers are unique within
    /// the result set.</remarks>
    /// <param name="signature">An array of unsigned integers representing the signature to query against the LSH buckets. The array must
    /// contain values compatible with the current LSH configuration.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A read-only collection of <see cref="Guid"/> values representing the identifiers of matching advertisements. The
    /// collection will be empty if no matches are found.</returns>
    public async Task<IReadOnlyCollection<Guid>> Query(uint[] signature, CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();

        for (int band = 0; band < _activeConfig.LshBandCount; band++)
        {
            var key = ComputeBandKey(signature, band, _activeConfig.RowsPerBand);
            var matches = await _dbContext.LshBuckets
                .AsNoTracking()
                .Where(x => x.Version == _activeConfig.Version && x.Band == band && x.Key == key)
                .Select(x => x.AdId)
                .ToListAsync(cancellationToken);

            foreach (var id in matches)
                ids.Add(id);
        }

        return [.. ids];
    }


    private LshBucketEntity ComputeBand(uint[] signature, int band, in Guid adId, in DateTime timeStamp) 
        => new()
        {
            Id = Guid.CreateVersion7(),
            Version = _activeConfig.Version,
            Band = band,
            Key = ComputeBandKey(signature, band, _activeConfig.RowsPerBand),
            AdId = adId,
            CreatedAt = timeStamp
        };


    private static string ComputeBandKey(uint[] signature, int bandIndex, int rowsPerBand)
    {
        var start = bandIndex * rowsPerBand;
        var end = Math.Min(start + rowsPerBand, signature.Length);
        uint hash = 2166136261;
        for (int i = start; i < end; i++)
        {
            hash ^= signature[i];
            hash *= 16777619;
        }

        return hash.ToString("X8");
    }


    private readonly VectorizationModelParams _activeConfig;
    private readonly ApplicationDbContext _dbContext;
}
