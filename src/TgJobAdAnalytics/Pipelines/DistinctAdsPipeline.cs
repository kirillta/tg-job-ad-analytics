using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Pipelines;

/// <summary>
/// Pipeline that deduplicates ads by computing master-vectors and writing them to the persistent index.
/// </summary>
public sealed class DistinctAdsPipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DistinctAdsPipeline"/> class.
    /// </summary>
    public DistinctAdsPipeline(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, SimilarityCalculator similarityCalculator)
    {
        _logger = loggerFactory.CreateLogger<DistinctAdsPipeline>();
        _dbContext = dbContext;
        _similarityCalculator = similarityCalculator;
    }


    /// <inheritdoc/>
    public string Description 
        => "Compute master-vectors (MinHash) and persist index to deduplicate ads; keeps only unique ones.";


    /// <inheritdoc/>
    public bool IsIdempotent 
        => true;


    /// <inheritdoc/>
    public string Name
        => "distinct-ads";


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var ads = await _dbContext.Ads
            .AsNoTracking()
            .Select(a => a)
            .ToListAsync(cancellationToken);
        if (ads.Count == 0) 
            return 0;

        var uniques = await _similarityCalculator.DistinctPersistent(ads, cancellationToken);
        _logger.LogInformation("DistinctAdsPipeline: unique {Unique}/{Total}", uniques.Count, ads.Count);

        return uniques.Count;
    }


    private readonly ILogger<DistinctAdsPipeline> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly SimilarityCalculator _similarityCalculator;
}
