using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Models.Pipelines;

namespace TgJobAdAnalytics.Services.Pipelines;

/// <summary>
/// Default pipeline runner that resolves pipelines from DI and executes them by name.
/// </summary>
public sealed class PipelineRunner : IPipelineRunner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineRunner"/> class.
    /// </summary>
    public PipelineRunner(ILoggerFactory loggerFactory, IEnumerable<IPipeline> pipelines)
    {
        _logger = loggerFactory.CreateLogger<PipelineRunner>();
        _pipelinesByName = pipelines.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }


    /// <inheritdoc/>
    public IEnumerable<PipelineMetadata> List()
    {
        foreach (var p in _pipelinesByName.Values.OrderBy(p => p.Name))
            yield return new PipelineMetadata(p.Name, p.Description, p.IsIdempotent);
    }


    /// <inheritdoc/>
    public async Task<int> Run(string name, CancellationToken cancellationToken)
    {
        if (!_pipelinesByName.TryGetValue(name, out var pipeline))
            throw new InvalidOperationException($"Pipeline '{name}' not found. Available: {string.Join(", ", _pipelinesByName.Keys.OrderBy(x => x))}");

        _logger.LogInformation("Running pipeline '{Name}' ({Description})", pipeline.Name, pipeline.Description);

        var start = DateTime.UtcNow;
        var result = await pipeline.Run(cancellationToken);
        var elapsed = DateTime.UtcNow - start;

        _logger.LogInformation("Pipeline '{Name}' completed. Processed: {Count}. Elapsed: {ElapsedMs} ms", pipeline.Name, result, (int)elapsed.TotalMilliseconds);
        return result;
    }


    private readonly ILogger<PipelineRunner> _logger;
    private readonly Dictionary<string, IPipeline> _pipelinesByName;
}
