using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// EF-based LSH index (banded hashing) over signatures.
/// </summary>
public sealed class VectorIndex : IVectorIndex
{
    public VectorIndex(ApplicationDbContext db, IVectorizationConfig config)
    {
        _db = db;
        _config = config;
    }

    public async Task Upsert(Guid adId, uint[] signature, CancellationToken cancellationToken)
    {
        var p = _config.GetActive();

        for (int band = 0; band < p.LshBandCount; band++)
        {
            var key = ComputeBandKey(signature, band, p.RowsPerBand);
            var entity = new LshBucketEntity
            {
                Id = Guid.CreateVersion7(),
                Version = p.Version,
                Band = band,
                Key = key,
                AdId = adId,
                CreatedAt = DateTime.UtcNow
            };
            _db.LshBuckets.Add(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }


    public async Task<IReadOnlyCollection<Guid>> Query(uint[] signature, CancellationToken cancellationToken)
    {
        var p = _config.GetActive();
        var ids = new HashSet<Guid>();

        for (int band = 0; band < p.LshBandCount; band++)
        {
            var key = ComputeBandKey(signature, band, p.RowsPerBand);
            var matches = await _db.LshBuckets
                .AsNoTracking()
                .Where(x => x.Version == p.Version && x.Band == band && x.Key == key)
                .Select(x => x.AdId)
                .ToListAsync(cancellationToken);

            foreach (var id in matches)
                ids.Add(id);
        }

        return ids.ToList();
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


    private readonly ApplicationDbContext _db;
    private readonly IVectorizationConfig _config;
}
