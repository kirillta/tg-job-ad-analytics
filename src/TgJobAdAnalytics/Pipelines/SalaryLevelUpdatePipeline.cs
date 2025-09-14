using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Services.Levels;

namespace TgJobAdAnalytics.Pipelines;

/// <summary>
/// Pipeline that backfills or refreshes salary position levels.
/// </summary>
public sealed class SalaryLevelUpdatePipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryLevelUpdatePipeline"/> class.
    /// </summary>
    public SalaryLevelUpdatePipeline(ILoggerFactory loggerFactory, SalaryLevelUpdateProcessor processor)
    {
        _logger = loggerFactory.CreateLogger<SalaryLevelUpdatePipeline>();
        _processor = processor;
    }


    /// <inheritdoc/>
    public string Name => "update-levels";


    /// <inheritdoc/>
    public string Description => "Backfill missing salary levels based on tags and ad text.";


    /// <inheritdoc/>
    public bool IsIdempotent => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        return await _processor.UpdateMissingLevels(cancellationToken);
    }


    private readonly ILogger<SalaryLevelUpdatePipeline> _logger;
    private readonly SalaryLevelUpdateProcessor _processor;
}
