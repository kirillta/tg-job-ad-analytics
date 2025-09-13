using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Vectors;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Pipelines;

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
        ApplicationDbContext db,
        IOptions<VectorizationOptions> options,
        IVectorizationConfig config,
        VectorsBackfillService backfill)
    {
        _logger = loggerFactory.CreateLogger<InitVectorsPipeline>();
        _db = db;
        _options = options.Value;
        _config = config;
        _backfill = backfill;
    }


    /// <inheritdoc/>
    public string Name => "init-vectors";


    /// <inheritdoc/>
    public string Description => "Seed active vector model and backfill master-vectors and LSH buckets.";


    /// <inheritdoc/>
    public bool IsIdempotent => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var activeModelConfig = _config.GetActive();
        var model = await _db.VectorModelVersions.FirstOrDefaultAsync(x => x.Version == activeModelConfig.Version, cancellationToken);
        if (model is null)
        {
            model = new VectorModelVersionEntity
            {
                Version = activeModelConfig.Version,
                NormalizationVersion = _options.NormalizationVersion,
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

            _db.VectorModelVersions.Add(model);
            await _db.SaveChangesAsync(cancellationToken);

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
                _db.VectorModelVersions.Update(model);
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("InitVectors: updated model version {Version} (IsActive={IsActive}, VocabularySize={VocabularySize})", model.Version, model.IsActive, model.VocabularySize);
            }
        }

        var processed = await _backfill.Backfill(cancellationToken);

        _logger.LogInformation("InitVectors: backfilled vectors for {Count} ads", processed);
        return processed;
    }


    private readonly ILogger<InitVectorsPipeline> _logger;
    private readonly ApplicationDbContext _db;
    private readonly VectorizationOptions _options;
    private readonly IVectorizationConfig _config;
    private readonly VectorsBackfillService _backfill;
}
