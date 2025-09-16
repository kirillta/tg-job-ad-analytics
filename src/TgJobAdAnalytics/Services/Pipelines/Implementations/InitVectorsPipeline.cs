using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Vectors;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Services.Pipelines.Implementations;

/// <summary>
/// Seeds the active vectorization model into the DB (if missing) and backfills vectors and index entries.
/// </summary>
public sealed class InitVectorsPipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitVectorsPipeline"/> class.
    /// </summary>
    public InitVectorsPipeline(
        ILoggerFactory loggerFactory,
        ApplicationDbContext dbContext,
        IOptions<VectorizationOptions> vectorizationOptions,
        OptionVectorizationConfig vectorizationConfig,
        VectorsBackfillService vectorsBackfillService)
    {
        _logger = loggerFactory.CreateLogger<InitVectorsPipeline>();
        _dbContext = dbContext;
        _vectorizationOptions = vectorizationOptions.Value;
        _vectorizationConfig = vectorizationConfig;
        _vectorsBackfillService = vectorsBackfillService;
    }


    /// <inheritdoc/>
    public string Name 
        => "init-vectors";


    /// <inheritdoc/>
    public string Description
        => "Seed active vector model and backfill master-vectors and LSH buckets.";


    /// <inheritdoc/>
    public bool IsIdempotent 
        => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var activeModelConfig = _vectorizationConfig.GetActive();
        var model = await _dbContext.VectorModelVersions.FirstOrDefaultAsync(x => x.Version == activeModelConfig.Version, cancellationToken);
        if (model is null)
        {
            model = new VectorModelVersionEntity
            {
                Version = activeModelConfig.Version,
                NormalizationVersion = _vectorizationOptions.NormalizationVersion,
                ShingleSize = activeModelConfig.ShingleSize,
                HashFunctionCount = activeModelConfig.HashFunctionCount,
                MinHashSeed = activeModelConfig.MinHashSeed,
                LshBandCount = activeModelConfig.LshBandCount,
                LshRowsPerBand = activeModelConfig.RowsPerBand,
                VocabularySize = activeModelConfig.VocabularySize,
                DuplicateThreshold = 0.92,
                SimilarThreshold = 0.80,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VectorModelVersions.Add(model);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("InitVectors: seeded model version {Version}", activeModelConfig.Version);
        }
        else
        {
            bool needsUpdate = false;
            if (!model.IsActive) 
            { 
                model.IsActive = true; 
                needsUpdate = true; 
            }

            if (model.VocabularySize != activeModelConfig.VocabularySize) 
            { 
                model.VocabularySize = activeModelConfig.VocabularySize; 
                needsUpdate = true; 
            }

            if (needsUpdate)
            {
                _dbContext.VectorModelVersions.Update(model);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("InitVectors: updated model version {Version} (IsActive={IsActive}, VocabularySize={VocabularySize})", model.Version, model.IsActive, model.VocabularySize);
            }
        }

        var processed = await _vectorsBackfillService.Backfill(cancellationToken);

        _logger.LogInformation("InitVectors: backfilled vectors for {Count} ads", processed);
        return processed;
    }

    
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<InitVectorsPipeline> _logger;
    private readonly VectorsBackfillService _vectorsBackfillService;
    private readonly OptionVectorizationConfig _vectorizationConfig;
    private readonly VectorizationOptions _vectorizationOptions;
}
