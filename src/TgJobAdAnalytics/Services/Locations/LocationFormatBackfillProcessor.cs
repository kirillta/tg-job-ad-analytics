using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Locations.Enums;

namespace TgJobAdAnalytics.Services.Locations;

/// <summary>
/// Backfills vacancy location and work format for existing ads that have not yet been classified.
/// Processes only ads where both <see cref="VacancyLocation"/> and <see cref="WorkFormat"/> are <c>Unknown</c>.
/// </summary>
public sealed class LocationFormatBackfillProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationFormatBackfillProcessor"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create a logger.</param>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="locationFormatExtractionService">Service used to classify location and work format via LLM.</param>
    public LocationFormatBackfillProcessor(
        ILoggerFactory loggerFactory,
        ApplicationDbContext dbContext,
        LocationFormatExtractionService locationFormatExtractionService)
    {
        _logger = loggerFactory.CreateLogger<LocationFormatBackfillProcessor>();
        _dbContext = dbContext;
        _locationFormatExtractionService = locationFormatExtractionService;
    }


    /// <summary>
    /// Classifies location and work format for all ads where both values are currently <c>Unknown</c>.
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

        var totalUpdated = 0;
        var processed = 0;

        foreach (var chunk in ads.Chunk(50))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkUpdates = new List<(Guid Id, VacancyLocation Location, WorkFormat Format)>();

            foreach (var ad in chunk)
            {
                var (location, format) = await _locationFormatExtractionService.Process(ad.Text, cancellationToken);

                if (location == VacancyLocation.Unknown && format == WorkFormat.Unknown)
                    continue;

                chunkUpdates.Add((ad.Id, location, format));
            }

            processed += chunk.Length;

            if (chunkUpdates.Count > 0)
            {
                foreach (var group in chunkUpdates.GroupBy(u => (u.Location, u.Format)))
                {
                    var location = group.Key.Location;
                    var format = group.Key.Format;
                    var ids = group.Select(u => u.Id).Distinct().ToList();

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
            }

            Console.Write(string.Empty);
            _logger.LogInformation("Processed {Processed}/{Total} ads; total updated: {Updated}", processed, ads.Count, totalUpdated);
        }

        _logger.LogInformation("Location/format backfill complete. Updated {Count} ads", totalUpdated);
        return totalUpdated;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly LocationFormatExtractionService _locationFormatExtractionService;
    private readonly ILogger<LocationFormatBackfillProcessor> _logger;
}
