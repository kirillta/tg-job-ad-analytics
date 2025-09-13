namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// LSH index persistence and query API.
/// </summary>
public interface IVectorIndex
{
    Task<IReadOnlyCollection<Guid>> Query(uint[] signature, CancellationToken cancellationToken);

    Task Upsert(Guid adId, uint[] signature, CancellationToken cancellationToken);
}
