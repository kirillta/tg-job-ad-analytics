using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Uploads;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryExtractionProcessor
{
    public SalaryExtractionProcessor(ApplicationDbContext dbContext, SalaryExtractionService salaryExtractionService, SalaryPersistenceService salaryPersistenceService, IOptions<UploadOptions> uploadOptions)
    {
        const int minimumBatchSize = 20;
        var batchSize = uploadOptions.Value.BatchSize / 200;
        _batchSize = batchSize < minimumBatchSize 
            ? minimumBatchSize 
            : batchSize;
        
        _dbContext = dbContext;
        _salaryExtractionService = salaryExtractionService;
        _salaryPersistenceService = salaryPersistenceService;
    }


    public async Task ExtractAndPersist(CancellationToken cancellationToken)
    {
        await _salaryPersistenceService.Initialize(cancellationToken);

        var channel = Channel.CreateBounded<SalaryEntity>(new BoundedChannelOptions(_batchSize * 2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var persistenceTask = ConsumeAndPersistBatches(channel.Reader, cancellationToken);

        var ads = await GetAdsWithoutSalaries(cancellationToken);
        foreach (var ad in ads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var messageTags = await GetMessageTags(ad.MessageId, cancellationToken);
            var salaryEntry = await _salaryExtractionService.Process(ad, messageTags, cancellationToken);
            
            if (salaryEntry is not null)
                await channel.Writer.WriteAsync(salaryEntry!, cancellationToken);
        }

        channel.Writer.Complete();
        await persistenceTask;


        async Task ConsumeAndPersistBatches(ChannelReader<SalaryEntity> reader, CancellationToken cancellationToken)
        {
            var batch = new List<SalaryEntity>(_batchSize);

            await foreach (var salary in reader.ReadAllAsync(cancellationToken))
            {
                batch.Add(salary);

                if (batch.Count >= _batchSize)
                {
                    await _salaryPersistenceService.ProcessBatch(batch, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await _salaryPersistenceService.ProcessBatch(batch, cancellationToken);
        }
    }


    private async Task<List<AdEntity>> GetAdsWithoutSalaries(CancellationToken cancellationToken)
    {
        var existingSalaryAdIds = await _dbContext.Salaries
            .AsNoTracking()
            .Select(s => s.AdId)
            .ToHashSetAsync(cancellationToken);

        return await _dbContext.Ads
            .AsNoTracking()
            .Where(ad => !existingSalaryAdIds.Contains(ad.Id))
            .Select(x => x)
            .ToListAsync(cancellationToken);
    }


    private async Task<List<string>> GetMessageTags(Guid messageId, CancellationToken cancellationToken)
    {
        var tags = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.Id == messageId)
            .Select(m => m.Tags)
            .FirstOrDefaultAsync(cancellationToken);

        return tags ?? [];
    }

    
    private readonly int _batchSize;
    private readonly ApplicationDbContext _dbContext;
    private readonly SalaryExtractionService _salaryExtractionService;
    private readonly SalaryPersistenceService _salaryPersistenceService;
}
