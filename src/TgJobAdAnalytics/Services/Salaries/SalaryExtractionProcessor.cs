using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Locations.Enums;
using TgJobAdAnalytics.Models.OpenAI;
using TgJobAdAnalytics.Utils;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Orchestrates extraction of structured salary data from advertisement entities using an LLM-backed
/// <see cref="SalaryExtractionService"/>, persisting results in batches. Implements adaptive concurrency
/// via <see cref="AdaptiveRateLimiter"/> to optimize throughput while reacting to rate limits.
/// Streams ads in configurable chunks, processes them in parallel, and writes successful salary entities
/// to a channel consumed by the <see cref="SalaryPersistenceService"/>.
/// </summary>
public sealed class SalaryExtractionProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryExtractionProcessor"/>.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create loggers.</param>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="salaryExtractionService">Service that extracts salary information from ad text.</param>
    /// <param name="salaryPersistenceService">Service responsible for persisting extracted salary entities.</param>
    /// <param name="openAiOptions">OpenAI processing / throttling configuration.</param>
    public SalaryExtractionProcessor(
        ILoggerFactory loggerFactory, 
        ApplicationDbContext dbContext, 
        SalaryExtractionService salaryExtractionService, 
        SalaryPersistenceService salaryPersistenceService, 
        IOptions<OpenAiOptions> openAiOptions)
    {
        _logger = loggerFactory.CreateLogger<SalaryExtractionProcessor>();

        _dbContext = dbContext;
        _openAiOptions = openAiOptions.Value;
        _salaryExtractionService = salaryExtractionService;
        _salaryPersistenceService = salaryPersistenceService;
    }


    /// <summary>
    /// Extracts salaries for ads lacking salary data and persists them in batches. Uses an adaptive rate limiter
    /// to adjust parallelism based on success / rate limit feedback. Safe for repeated execution; only ads without
    /// existing salary records are processed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExtractAndPersist(CancellationToken cancellationToken)
    {
        using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!_dbContext.Ads.Any(ad => !_dbContext.Salaries.Any(s => s.AdId == ad.Id)))
        {
            _logger.LogInformation("No ads without salaries. Skipping extraction.");
            return;
        }

        await _salaryPersistenceService.Initialize(linkedCancellationSource.Token);

        var channel = Channel.CreateBounded<SalaryEntity>(new BoundedChannelOptions(_openAiOptions.ProcessingChunkSize * 2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var locationUpdates = new ConcurrentBag<(Guid AdId, VacancyLocation Location, WorkFormat Format)>();
        var persistenceTask = ConsumeAndPersistBatches(channel.Reader, locationUpdates, cancellationToken);

        var totalAds = await _dbContext.Ads.CountAsync(ad => !_dbContext.Salaries.Any(s => s.AdId == ad.Id), cancellationToken);
        _logger.LogInformation("Starting data extraction for {TotalAds} ads.", totalAds);

        using var rateLimiter = new AdaptiveRateLimiter(_openAiOptions);
        await foreach (var chunk in GetAdsInChunks(cancellationToken))
        {
            if (linkedCancellationSource.IsCancellationRequested)
                break;

            cancellationToken.ThrowIfCancellationRequested();

            var tagsByMessageId = await PreloadMessageTags([.. chunk.Select(ad => ad.MessageId)], cancellationToken);

            _logger.LogInformation("Processing chunk of {ChunkSize} ads with concurrency: {Concurrency}", chunk.Count, rateLimiter.CurrentConcurrency);

            await Parallel.ForEachAsync(chunk, new ParallelOptions
            {
                MaxDegreeOfParallelism = rateLimiter.CurrentConcurrency,
                CancellationToken = linkedCancellationSource.Token
            }, ProcessAd);


            async ValueTask ProcessAd(AdEntity ad, CancellationToken ct)
            {
                using var permit = await rateLimiter.Acquire(ct);

                try
                {
                    var messageTags = tagsByMessageId.GetValueOrDefault(ad.MessageId, []);
                    var result = await _salaryExtractionService.Process(ad, messageTags, ct);

                    rateLimiter.RecordSuccess();

                    locationUpdates.Add((ad.Id, result.Location, result.Format));

                    if (result.Salary is not null)
                        await channel.Writer.WriteAsync(result.Salary, ct);
                }
                catch (Exception ex)
                {
                    var isRateLimitError = RateLimitHelper.IsRateLimitException(ex);
                    rateLimiter.RecordFailure(isRateLimitError);

                    if (RateLimitHelper.IsQuotaExceeded(ex))
                    {
                        _logger.LogError(ex, "Quota exceeded. Cancelling processing.");
                        linkedCancellationSource.Cancel();

                        return;
                    }

                    if (isRateLimitError)
                    {
                        if (RateLimitHelper.TryParseRetryAfter(ex, out var retryAfter))
                            rateLimiter.RecordRetryAfter(delay: retryAfter);

                        return;
                    }

                    _logger.LogError(ex, "Failed to process ad {AdId}.", ad.Id);
                }
            }
        }

        channel.Writer.Complete();
        await persistenceTask;
    }


    private async Task ConsumeAndPersistBatches(
        ChannelReader<SalaryEntity> reader,
        ConcurrentBag<(Guid AdId, VacancyLocation Location, WorkFormat Format)> locationUpdates,
        CancellationToken cancellationToken)
    {
        var batch = new List<SalaryEntity>(_openAiOptions.ProcessingChunkSize);

        await foreach (var salary in reader.ReadAllAsync(cancellationToken))
        {
            batch.Add(salary);

            if (batch.Count >= _openAiOptions.ProcessingChunkSize)
            {
                await _salaryPersistenceService.ProcessBatch(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
            await _salaryPersistenceService.ProcessBatch(batch, cancellationToken);

        await BatchUpdateAdLocations(locationUpdates, cancellationToken);
    }


    private async Task BatchUpdateAdLocations(
        ConcurrentBag<(Guid AdId, VacancyLocation Location, WorkFormat Format)> updates,
        CancellationToken cancellationToken)
    {
        if (updates.IsEmpty)
            return;

        foreach (var group in updates.GroupBy(u => (u.Location, u.Format)))
        {
            var location = group.Key.Location;
            var format = group.Key.Format;

            if (location == VacancyLocation.Unknown && format == WorkFormat.Unknown)
                continue;

            var ids = group.Select(u => u.AdId).ToArray();

            foreach (var idChunk in ids.Chunk(500))
            {
                await _dbContext.Ads
                    .Where(a => idChunk.Contains(a.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(a => a.Location, location)
                        .SetProperty(a => a.WorkFormat, format), cancellationToken);
            }
        }
    }


    private async IAsyncEnumerable<List<AdEntity>> GetAdsInChunks([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chunk = new List<AdEntity>(_openAiOptions.ProcessingChunkSize);

        await foreach (var ad in GetAdsWithoutSalariesStream(cancellationToken))
        {
            chunk.Add(ad);

            if (chunk.Count >= _openAiOptions.ProcessingChunkSize)
            {
                yield return chunk;
                chunk = new List<AdEntity>(_openAiOptions.ProcessingChunkSize);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }


    private async IAsyncEnumerable<AdEntity> GetAdsWithoutSalariesStream([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.Ads
            .AsNoTracking()
            .Where(ad => !_dbContext.Salaries.Any(s => s.AdId == ad.Id))
            .AsAsyncEnumerable();

        await foreach (var ad in query.WithCancellation(cancellationToken))
            yield return ad;
    }


    private async Task<Dictionary<Guid, List<string>>> PreloadMessageTags(List<Guid> messageIds, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
            return [];

        return await _dbContext.Messages
            .AsNoTracking()
            .Where(m => messageIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Tags })
            .ToDictionaryAsync(m => m.Id, m => m.Tags ?? [], cancellationToken);
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryExtractionProcessor> _logger;
    private readonly OpenAiOptions _openAiOptions;
    private readonly SalaryExtractionService _salaryExtractionService;
    private readonly SalaryPersistenceService _salaryPersistenceService;
}
