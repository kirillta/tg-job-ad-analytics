using TgJobAdAnalytics.Data.Vectors;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Persistence API for ad vectors (master-vectors).
/// </summary>
public interface IVectorStore
{
    Task<AdVectorEntity?> Get(Guid adId, int version, CancellationToken cancellationToken);

    Task Upsert(Guid adId, uint[] signature, int shingleCount, CancellationToken cancellationToken);
}
