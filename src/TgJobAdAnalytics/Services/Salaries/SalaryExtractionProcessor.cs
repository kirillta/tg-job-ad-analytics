using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryExtractionProcessor
{
    public SalaryExtractionProcessor(ApplicationDbContext dbContext, SalaryExtractionService salaryExtractionService, SalaryPersistenceService salaryPersistenceService)
    {
        _dbContext = dbContext;
        _salaryExtractionService = salaryExtractionService;
        _salaryPersistenceService = salaryPersistenceService;
    }


    public async Task ExtractAndPersist(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<(AdEntity Ad, SalaryEntity? Salary)>();
        var persistenceTask = ConsumeAndPersist(channel.Reader, cancellationToken);
        
        var ads = await GetAdsWithoutSalaries(cancellationToken);
        foreach (var ad in ads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var salaryEntry = await _salaryExtractionService.Process(ad, cancellationToken);
            await channel.Writer.WriteAsync((ad, salaryEntry), cancellationToken);
        }

        channel.Writer.Complete();
        await persistenceTask;


        async Task ConsumeAndPersist(ChannelReader<(AdEntity Ad, SalaryEntity? Salary)> reader, CancellationToken cancellationToken)
        {
            await foreach (var (_, salary) in reader.ReadAllAsync(cancellationToken))
                await _salaryPersistenceService.Process(salary, cancellationToken);
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


    private readonly ApplicationDbContext _dbContext;
    private readonly SalaryExtractionService _salaryExtractionService;
    private readonly SalaryPersistenceService _salaryPersistenceService;
}
