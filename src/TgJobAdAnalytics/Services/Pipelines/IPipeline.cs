namespace TgJobAdAnalytics.Services.Pipelines;

/// <summary>
/// Represents a named, self-contained unit of work that can be executed on demand.
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// Gets a short human-readable description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Indicates whether running this pipeline multiple times is safe (no side effects beyond intended updates).
    /// </summary>
    bool IsIdempotent { get; }

    /// <summary>
    /// Gets the unique pipeline name used to select and execute it (e.g., "update-levels").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the pipeline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of processed/updated items.</returns>
    Task<int> Run(CancellationToken cancellationToken);
}
