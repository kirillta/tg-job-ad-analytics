using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Locations.Enums;
using TgJobAdAnalytics.Models.OpenAI;
using TgJobAdAnalytics.Utils;

namespace TgJobAdAnalytics.Services.Locations;

/// <summary>
/// Backfills vacancy location and work format for existing ads that have not yet been classified.
/// Processes only ads where both <see cref="VacancyLocation"/> and <see cref="WorkFormat"/> are <c>Unknown</c>.
/// Uses adaptive concurrency via <see cref="AdaptiveRateLimiter"/> to optimize throughput while reacting to rate limits.
/// </summary>
public sealed class LocationFormatBackfillProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationFormatBackfillProcessor"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create a logger.</param>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="locationFormatExtractionService">Service used to classify location and work format via LLM.</param>
    /// <param name="openAiOptions">OpenAI processing / throttling configuration.</param>
    public LocationFormatBackfillProcessor(
        ILoggerFactory loggerFactory,
        ApplicationDbContext dbContext,
        LocationFormatExtractionService locationFormatExtractionService,
        IOptions<OpenAiOptions> openAiOptions)
    {
        _logger = loggerFactory.CreateLogger<LocationFormatBackfillProcessor>();
        _dbContext = dbContext;
        _locationFormatExtractionService = locationFormatExtractionService;
        _openAiOptions = openAiOptions.Value;
    }


    /// <summary>
    /// Classifies location and work format for all ads where both values are currently <c>Unknown</c>.
    /// Ads that fail due to rate limiting remain <c>Unknown</c> and will be retried on subsequent runs.
    /// Aborts immediately if the OpenAI quota is exceeded.
    /// </summary>
    /// <returns>Number of ads updated.</returns>
    public async Task<int> BackfillMissing(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting location/format backfill");

        var ads = await _dbContext.Ads
            .Where(a => a.Location == VacancyLocation.Unknown && a.WorkFormat == WorkFormat.Unknown)
            .Select(a => new { a.Id, a.Text })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} ads with unknown location and work format", ads.Count);

        if (ads.Count == 0)
            return 0;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var rateLimiter = new AdaptiveRateLimiter(_openAiOptions);

        var processed = 0;
        var totalUpdated = 0;

        foreach (var chunk in ads.Chunk(_openAiOptions.ProcessingChunkSize))
        {
            linkedCts.Token.ThrowIfCancellationRequested();
            var updates = new ConcurrentBag<(Guid Id, VacancyLocation Location, WorkFormat Format)>();

            await Parallel.ForEachAsync(chunk, new ParallelOptions
            {
                MaxDegreeOfParallelism = rateLimiter.CurrentConcurrency,
                CancellationToken = linkedCts.Token
            }, async (ad, ct) =>
            {
                using var permit = await rateLimiter.Acquire(ct);

                try
                {
                    var (location, format) = await _locationFormatExtractionService.Process(ad.Text, ct);
                    rateLimiter.RecordSuccess();

                    if (location != VacancyLocation.Unknown || format != WorkFormat.Unknown)
                        updates.Add((ad.Id, location, format));
                }
                catch (Exception ex)
                {
                    if (RateLimitHelper.IsQuotaExceeded(ex))
                    {
                        _logger.LogError(ex, "Quota exceeded. Cancelling backfill.");
                        linkedCts.Cancel();
                        return;
                    }

                    var isRateLimitError = RateLimitHelper.IsRateLimitException(ex);
                    rateLimiter.RecordFailure(isRateLimitError);

                    if (isRateLimitError)
                    {
                        if (RateLimitHelper.TryParseRetryAfter(ex, out var retryAfter))
                            rateLimiter.RecordRetryAfter(delay: retryAfter);

                        return;
                    }

                    _logger.LogError(ex, "Failed to classify location/format for ad {AdId}.", ad.Id);
                }
            });

            processed += chunk.Length;
            totalUpdated += await BatchUpdateAdLocations(updates, cancellationToken);

            _logger.LogInformation("Processed {Processed}/{Total} ads; total updated: {Updated}", processed, ads.Count, totalUpdated);
        }

        _logger.LogInformation("Location/format backfill complete. Updated {Count} ads", totalUpdated);
        return totalUpdated;
    }


    private async Task<int> BatchUpdateAdLocations(ConcurrentBag<(Guid Id, VacancyLocation Location, WorkFormat Format)> updates, CancellationToken cancellationToken)
    {
        if (updates.IsEmpty)
            return 0;

        var totalUpdated = 0;

        foreach (var group in updates.GroupBy(u => (u.Location, u.Format)))
        {
            var location = group.Key.Location;
            var format = group.Key.Format;
            var ids = group.Select(u => u.Id).Distinct().ToArray();

            foreach (var idChunk in ids.Chunk(500))
            {
                var updated = await _dbContext.Ads
                    .Where(a => idChunk.Contains(a.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(a => a.Location, location)
                        .SetProperty(a => a.WorkFormat, format), cancellationToken);

                totalUpdated += updated;
            }
        }

        return totalUpdated;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly LocationFormatExtractionService _locationFormatExtractionService;
    private readonly ILogger<LocationFormatBackfillProcessor> _logger;
    private readonly OpenAiOptions _openAiOptions;
}
