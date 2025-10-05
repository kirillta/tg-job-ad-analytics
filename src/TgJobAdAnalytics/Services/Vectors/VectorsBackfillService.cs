using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Backfills master-vectors (MinHash signatures) and LSH index entries for ads missing vectors for the active model version.
/// </summary>
public sealed class VectorsBackfillService
{
    public VectorsBackfillService(
        ApplicationDbContext dbContext,
        MinHashVectorizer minHashVectorizer,
        VectorStore vectorStore,
        VectorIndex vectorIndex,
        OptionVectorizationConfig vectorizationConfig)
    {
        _dbContext = dbContext;
        _minHashVectorizer = minHashVectorizer;
        _vectorStore = vectorStore;
        _vectorIndex = vectorIndex;
        _vectorizationConfig = vectorizationConfig;
    }


    /// <summary>
    /// Compute and persist vectors/index for ads missing entries for the active version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processed ads count.</returns>
    public async Task<int> Backfill(CancellationToken cancellationToken)
    {
        var activeVersionConfig = _vectorizationConfig.GetActive();

        var existing = await _dbContext.AdVectors
            .AsNoTracking()
            .Where(v => v.Version == activeVersionConfig.Version)
            .Select(v => v.AdId)
            .ToHashSetAsync(cancellationToken);

        var ads = await _dbContext.Ads
            .AsNoTracking()
            .Where(ad => !existing.Contains(ad.Id))
            .Select(ad => new { ad.Id, ad.Text })
            .ToListAsync(cancellationToken);

        if (ads.Count == 0)
            return 0;

        var timeStamp = DateTime.UtcNow;
        var processed = 0;

        foreach ( var ad in ads) 
        { 
            var (sig, count) = _minHashVectorizer.Compute(ad.Text);

            await _vectorStore.Upsert(ad.Id, sig, count, timeStamp, cancellationToken);
            await _vectorIndex.Upsert(ad.Id, sig, timeStamp, cancellationToken);

            processed++;
        }

        return processed;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly MinHashVectorizer _minHashVectorizer;
    private readonly VectorStore _vectorStore;
    private readonly VectorIndex _vectorIndex;
    private readonly OptionVectorizationConfig _vectorizationConfig;
}
