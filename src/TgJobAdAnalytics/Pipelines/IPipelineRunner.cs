namespace TgJobAdAnalytics.Pipelines;

/// <summary>
/// Provides discovery and execution for configured pipelines.
/// </summary>
public interface IPipelineRunner
{
    /// <summary>
    /// Lists available pipelines.
    /// </summary>
    IEnumerable<PipelineMetadata> List();

    /// <summary>
    /// Runs a pipeline by name.
    /// </summary>
    /// <param name="name">Pipeline name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of processed/updated items.</returns>
    Task<int> Run(string name, CancellationToken cancellationToken);
}

/// <summary>
/// Describes a pipeline registered in the system.
/// </summary>
public sealed record PipelineMetadata(string Name, string Description, bool IsIdempotent);
