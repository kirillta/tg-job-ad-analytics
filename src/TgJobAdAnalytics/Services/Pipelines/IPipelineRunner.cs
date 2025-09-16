using TgJobAdAnalytics.Models.Pipelines;

namespace TgJobAdAnalytics.Services.Pipelines;

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
