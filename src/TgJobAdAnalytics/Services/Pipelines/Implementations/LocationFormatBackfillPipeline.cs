using TgJobAdAnalytics.Services.Locations;

namespace TgJobAdAnalytics.Services.Pipelines.Implementations;

/// <summary>
/// Pipeline that backfills vacancy location and work format for existing ads that have not yet been classified.
/// </summary>
public sealed class LocationFormatBackfillPipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationFormatBackfillPipeline"/> class.
    /// </summary>
    public LocationFormatBackfillPipeline(LocationFormatBackfillProcessor processor)
    {
        _processor = processor;
    }


    /// <inheritdoc/>
    public string Name
        => "backfill-locations";


    /// <inheritdoc/>
    public string Description
        => "Backfill vacancy location and work format for ads not yet classified.";


    /// <inheritdoc/>
    public bool IsIdempotent
        => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        return await _processor.BackfillMissing(cancellationToken);
    }


    private readonly LocationFormatBackfillProcessor _processor;
}
