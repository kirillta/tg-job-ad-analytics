using TgJobAdAnalytics.Services.Levels;

namespace TgJobAdAnalytics.Services.Pipelines.Implementations;

/// <summary>
/// Pipeline that backfills or refreshes salary position levels.
/// </summary>
public sealed class SalaryLevelUpdatePipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryLevelUpdatePipeline"/> class.
    /// </summary>
    public SalaryLevelUpdatePipeline(SalaryLevelUpdateProcessor salaryLevelUpdateProcessor)
    {
        _salaryLevelUpdateProcessor = salaryLevelUpdateProcessor;
    }


    /// <inheritdoc/>
    public string Name 
        => "update-levels";


    /// <inheritdoc/>
    public string Description 
        => "Backfill missing salary levels based on tags and ad text.";


    /// <inheritdoc/>
    public bool IsIdempotent 
        => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        return await _salaryLevelUpdateProcessor.UpdateMissingLevels(cancellationToken);
    }


    private readonly SalaryLevelUpdateProcessor _salaryLevelUpdateProcessor;
}
