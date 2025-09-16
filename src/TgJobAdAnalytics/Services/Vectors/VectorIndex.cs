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
        _dbContext = dbContext;
        _vectorizationConfig = vectorizationConfig;
    }


    public async Task Upsert(Guid adId, uint[] signature, CancellationToken cancellationToken)
    {
        var activeConfig = _vectorizationConfig.GetActive();

        for (int band = 0; band < activeConfig.LshBandCount; band++)
        {
            var key = ComputeBandKey(signature, band, activeConfig.RowsPerBand);
            var entity = new LshBucketEntity
            {
                Id = Guid.CreateVersion7(),
                Version = activeConfig.Version,
                Band = band,
                Key = key,
                AdId = adId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.LshBuckets.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<IReadOnlyCollection<Guid>> Query(uint[] signature, CancellationToken cancellationToken)
    {
        var activeConfig = _vectorizationConfig.GetActive();
        var ids = new HashSet<Guid>();

        for (int band = 0; band < activeConfig.LshBandCount; band++)
        {
            var key = ComputeBandKey(signature, band, activeConfig.RowsPerBand);
            var matches = await _dbContext.LshBuckets
                .AsNoTracking()
                .Where(x => x.Version == activeConfig.Version && x.Band == band && x.Key == key)
                .Select(x => x.AdId)
                .ToListAsync(cancellationToken);

            foreach (var id in matches)
                ids.Add(id);
        }

        return [.. ids];
    }


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


    private readonly ApplicationDbContext _dbContext;
    private readonly OptionVectorizationConfig _vectorizationConfig;
}
