using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// EF-based implementation of IVectorStore.
/// </summary>
public sealed class VectorStore : IVectorStore
{
    public VectorStore(ApplicationDbContext db, IVectorizationConfig config)
    {
        _db = db;
        _config = config;
    }

    public async Task Upsert(Guid adId, uint[] signature, int shingleCount, CancellationToken cancellationToken)
    {
        var p = _config.GetActive();
        var bytes = SignatureSerializer.ToBytes(signature);
        var hash = SignatureSerializer.Sha256Hex(bytes);

        var existing = await _db.AdVectors.FirstOrDefaultAsync(x => x.AdId == adId && x.Version == p.Version, cancellationToken);
        if (existing is null)
        {
            var entity = new AdVectorEntity
            {
                Id = Guid.CreateVersion7(),
                AdId = adId,
                Version = p.Version,
                Dim = signature.Length,
                Signature = bytes,
                SignatureHash = hash,
                ShingleCount = shingleCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.AdVectors.Add(entity);
        }
        else
        {
            existing.Dim = signature.Length;
            existing.Signature = bytes;
            existing.SignatureHash = hash;
            existing.ShingleCount = shingleCount;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.AdVectors.Update(existing);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }


    public Task<AdVectorEntity?> Get(Guid adId, int version, CancellationToken cancellationToken)
        => _db.AdVectors.AsNoTracking().FirstOrDefaultAsync(x => x.AdId == adId && x.Version == version, cancellationToken);


    private readonly ApplicationDbContext _db;
    private readonly IVectorizationConfig _config;
}
