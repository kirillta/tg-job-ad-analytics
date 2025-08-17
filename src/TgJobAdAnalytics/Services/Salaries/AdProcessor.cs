using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class AdProcessor
{
    public AdProcessor(ApplicationDbContext dbContext, SalaryExtractionService salaryExtractionService, SalaryPersistenceService salaryPersistenceService)
    {
        _dbContext = dbContext;
        _salaryExtractionService = salaryExtractionService;
        _salaryPersistenceService = salaryPersistenceService;
    }


    public async Task Process()
    {
        var channel = Channel.CreateUnbounded<(AdEntity Ad, SalaryEntity? Salary)>();
        var persistenceTask = ConsumeAndPersist(channel.Reader);
        
        var ads = await GetAds();
        foreach (var ad in ads)
        {
            var salaryEntry = await _salaryExtractionService.Process(ad);
            await channel.Writer.WriteAsync((ad, salaryEntry));
        
        }

        channel.Writer.Complete();
        await persistenceTask;


        async Task ConsumeAndPersist(ChannelReader<(AdEntity Ad, SalaryEntity? Salary)> reader)
        {
            await foreach (var (_, salary) in reader.ReadAllAsync())
                await _salaryPersistenceService.Process(salary);
        }
    }


    private async Task<List<AdEntity>> GetAds()
    {
        var existingSalaryAdIds = await _dbContext.Salaries
        .AsNoTracking()
        .Select(s => s.AdId)
        .ToHashSetAsync();

        return await _dbContext.Ads
            .AsNoTracking()
            .Where(ad => !existingSalaryAdIds.Contains(ad.Id))
            .Select(x => x)
            .ToListAsync();
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly SalaryExtractionService _salaryExtractionService;
    private readonly SalaryPersistenceService _salaryPersistenceService;
}
