using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;
using TgJobAdAnalytics.Models.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Provides persistence operations for advertisement MinHash signatures (vector representations).
/// Supports idempotent upsert of individual or batched vectors for the active vectorization model version.
/// </summary>
public sealed class VectorStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStore"/> resolving the active vectorization configuration.
    /// </summary>
    /// <param name="dbContext">EF Core database context.</param>
    /// <param name="vectorizationConfig">Configuration provider used to obtain the active vectorization model parameters.</param>
    public VectorStore(ApplicationDbContext dbContext, OptionVectorizationConfig vectorizationConfig)
    {
        _activeConfig = vectorizationConfig.GetActive();
        _dbContext = dbContext;
    }


    /// <summary>
    /// Inserts or updates (idempotent) a vector entry for a single advertisement and persists changes immediately.
    /// </summary>
    /// <param name="adId">Advertisement identifier.</param>
    /// <param name="signature">Raw MinHash signature values.</param>
    /// <param name="shingleCount">Number of shingles that produced the signature.</param>
    /// <param name="timeStamp">Timestamp applied to created/updated audit fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Upsert(Guid adId, uint[] signature, int shingleCount, DateTime timeStamp, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AdVectors
            .FirstOrDefaultAsync(x => x.AdId == adId && x.Version == _activeConfig.Version, cancellationToken);

        UpsertInternal(existing, in adId, signature, shingleCount, in timeStamp);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    /// <summary>
    /// Inserts or updates a batch of vector entries without saving changes (caller is responsible for SaveChanges).
    /// Efficiently fetches only existing records for the specified advertisement ids.
    /// </summary>
    /// <param name="items">Collection of tuples representing vectors to upsert.</param>
    /// <param name="timeStamp">Timestamp applied to created/updated audit fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpsertBatchWithoutSave(IReadOnlyList<(Guid AdId, uint[] Signature, int ShingleCount)> items, DateTime timeStamp, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var adIds = items.Select(x => x.AdId).ToList();

        var existingDict = await _dbContext.AdVectors
            .Where(x => adIds.Contains(x.AdId) && x.Version == _activeConfig.Version)
            .ToDictionaryAsync(x => x.AdId, cancellationToken);

        foreach (var (adId, signature, shingleCount) in items)
        {
            _ = existingDict.TryGetValue(adId, out var existing);
            UpsertInternal(existing, in adId, signature, shingleCount, in timeStamp);
        }
    }


    /// <summary>
    /// Retrieves an advertisement vector entity for a given ad id and model version (no tracking).
    /// </summary>
    /// <param name="adId">Advertisement identifier.</param>
    /// <param name="version">Vectorization model version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vector entity or null if not found.</returns>
    public Task<AdVectorEntity?> Get(Guid adId, int version, CancellationToken cancellationToken)
        => _dbContext.AdVectors.AsNoTracking().FirstOrDefaultAsync(x => x.AdId == adId && x.Version == version, cancellationToken);


    /// <summary>
    /// Retrieves advertisement vector entities for a batch of ad ids and a given model version, chunked to stay within
    /// SQLite's SQL variable limit (no tracking).
    /// </summary>
    /// <param name="adIds">Advertisement identifiers to look up.</param>
    /// <param name="version">Vectorization model version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping ad id to vector entity for found records.</returns>
    public async Task<Dictionary<Guid, AdVectorEntity>> GetBatch(IReadOnlyList<Guid> adIds, int version, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, AdVectorEntity>(adIds.Count);

        foreach (var chunk in adIds.Chunk(SqliteMaxVariables))
        {
            var rows = await _dbContext.AdVectors
                .AsNoTracking()
                .Where(x => chunk.Contains(x.AdId) && x.Version == version)
                .ToDictionaryAsync(x => x.AdId, cancellationToken);

            foreach (var kvp in rows)
                result[kvp.Key] = kvp.Value;
        }

        return result;
    }


    private static AdVectorEntity AddInternal(in Guid adId, uint[] signature, byte[] bytes, string hash, int shingleCount, in DateTime timeStamp, int configVersion)
        => new()
        {
            Id = Guid.CreateVersion7(),
            AdId = adId,
            Version = configVersion,
            Dim = signature.Length,
            Signature = bytes,
            SignatureHash = hash,
            ShingleCount = shingleCount,
            CreatedAt = timeStamp,
            UpdatedAt = timeStamp
        };


    private static AdVectorEntity UpdateInternal(AdVectorEntity entity, uint[] signature, byte[] bytes, string hash, int shingleCount, in DateTime timeStamp)
    {
        entity.Dim = signature.Length;
        entity.Signature = bytes;
        entity.SignatureHash = hash;
        entity.ShingleCount = shingleCount;
        entity.UpdatedAt = timeStamp;

        return entity;
    }


    private void UpsertInternal(AdVectorEntity? existing, in Guid adId, uint[] signature, int shingleCount, in DateTime timeStamp)
    {
        var bytes = SignatureSerializer.ToBytes(signature);
        var hash = SignatureSerializer.Sha256Hex(bytes);

        if (existing is null)
        {
            var entity = AddInternal(adId, signature, bytes, hash, shingleCount, timeStamp, _activeConfig.Version);
            _dbContext.AdVectors.Add(entity);
        }
        else
        {
            existing = UpdateInternal(existing, signature, bytes, hash, shingleCount, timeStamp);
            _dbContext.AdVectors.Update(existing);
        }
    }

    
    private const int SqliteMaxVariables = 900;

    private readonly VectorizationModelParams _activeConfig;
    private readonly ApplicationDbContext _dbContext;
}
