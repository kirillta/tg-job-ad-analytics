using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;
using TgJobAdAnalytics.Models.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

public sealed class VectorStore
{
    public VectorStore(ApplicationDbContext dbContext, OptionVectorizationConfig vectorizationConfig)
    {
        _dbContext = dbContext;
        _vectorizationConfig = vectorizationConfig;
    }


    public async Task Upsert(Guid adId, uint[] signature, int shingleCount, CancellationToken cancellationToken)
    {
        var activeConfig = _vectorizationConfig.GetActive();
        var bytes = SignatureSerializer.ToBytes(signature);
        var hash = SignatureSerializer.Sha256Hex(bytes);

        var existing = await _dbContext.AdVectors
            .FirstOrDefaultAsync(x => x.AdId == adId && x.Version == activeConfig.Version, cancellationToken);
        if (existing is null)
        {
            var entity = new AdVectorEntity
            {
                Id = Guid.CreateVersion7(),
                AdId = adId,
                Version = activeConfig.Version,
                Dim = signature.Length,
                Signature = bytes,
                SignatureHash = hash,
                ShingleCount = shingleCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.AdVectors.Add(entity);
        }
        else
        {
            existing.Dim = signature.Length;
            existing.Signature = bytes;
            existing.SignatureHash = hash;
            existing.ShingleCount = shingleCount;
            existing.UpdatedAt = DateTime.UtcNow;
            _dbContext.AdVectors.Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public Task<AdVectorEntity?> Get(Guid adId, int version, CancellationToken cancellationToken)
        => _dbContext.AdVectors.AsNoTracking().FirstOrDefaultAsync(x => x.AdId == adId && x.Version == version, cancellationToken);


    private readonly ApplicationDbContext _dbContext;
    private readonly OptionVectorizationConfig _vectorizationConfig;
}
