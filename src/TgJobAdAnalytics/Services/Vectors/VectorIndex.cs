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


    public async Task Upsert(Guid adId, uint[] signature, DateTime timeStamp, CancellationToken cancellationToken)
    {
        for (var band = 0; band < _activeConfig.LshBandCount; band++)
        {
            var entity = ComputeBand(signature, band, in adId, in timeStamp);
            _dbContext.LshBuckets.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


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


    private VectorizationModelParams _activeConfig;
    private readonly ApplicationDbContext _dbContext;
}
