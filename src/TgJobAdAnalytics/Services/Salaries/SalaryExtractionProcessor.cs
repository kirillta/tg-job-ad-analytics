using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.OpenAI;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryExtractionProcessor
{
    public SalaryExtractionProcessor(
        ILoggerFactory loggerFactory, 
        ApplicationDbContext dbContext, 
        SalaryExtractionService salaryExtractionService, 
        SalaryPersistenceService salaryPersistenceService, 
        IOptions<OpenAiOptions> openAiOptions)
    {
        _logger = loggerFactory.CreateLogger<SalaryExtractionProcessor>();
        _loggerFactory = loggerFactory;

        _dbContext = dbContext;
        _openAiOptions = openAiOptions.Value;
        _salaryExtractionService = salaryExtractionService;
        _salaryPersistenceService = salaryPersistenceService;
    }


    public async Task ExtractAndPersist(CancellationToken cancellationToken)
    {
        await _salaryPersistenceService.Initialize(cancellationToken);

        var channel = Channel.CreateBounded<SalaryEntity>(new BoundedChannelOptions(_openAiOptions.ProcessingChunkSize * 2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var persistenceTask = ConsumeAndPersistBatches(channel.Reader, cancellationToken);

        using var rateLimiter = new AdaptiveRateLimiter(_loggerFactory, _openAiOptions);

        await foreach (var chunk in GetAdsInChunks(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tagsByMessageId = await PreloadMessageTags([.. chunk.Select(ad => ad.MessageId)], cancellationToken);

            _logger.LogInformation("Processing chunk of {ChunkSize} ads with concurrency: {Concurrency}", chunk.Count, rateLimiter.CurrentConcurrency);

            await Parallel.ForEachAsync(chunk, new ParallelOptions
            {
                MaxDegreeOfParallelism = rateLimiter.CurrentConcurrency,
                CancellationToken = cancellationToken
            },
            async (ad, ct) =>
            {
                using var _ = await rateLimiter.AcquireAsync(ct);

                try
                {
                    var messageTags = tagsByMessageId.GetValueOrDefault(ad.MessageId, []);
                    var salaryEntry = await _salaryExtractionService.Process(ad, messageTags, ct);

                    rateLimiter.RecordSuccess();

                    if (salaryEntry is not null)
                        await channel.Writer.WriteAsync(salaryEntry, ct);
                }
                catch (Exception ex)
                {
                    var isRateLimitError = IsRateLimitException(ex);
                    rateLimiter.RecordFailure(isRateLimitError);

                    _logger.LogError(ex, "Failed to process ad {AdId}. Rate limit error: {IsRateLimit}", ad.Id, isRateLimitError);

                    if (isRateLimitError)
                        await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            });
        }

        channel.Writer.Complete();
        await persistenceTask;


        async Task ConsumeAndPersistBatches(ChannelReader<SalaryEntity> reader, CancellationToken cancellationToken)
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


    private static bool IsRateLimitException(Exception ex)
    {
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("rate limit") || 
            message.Contains("429") || 
            ex.GetType().Name.Contains("RateLimit", StringComparison.OrdinalIgnoreCase);
    }

    
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryExtractionProcessor> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly SalaryExtractionService _salaryExtractionService;
    private readonly SalaryPersistenceService _salaryPersistenceService;
}
