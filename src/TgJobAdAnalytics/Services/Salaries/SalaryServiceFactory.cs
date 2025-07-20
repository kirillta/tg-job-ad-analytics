using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryServiceFactory
{
    public SalaryServiceFactory(ApplicationDbContext dbContext, RateServiceFactory rateServiceFactory) 
    {
        _dbContext = dbContext;
        _rateServiceFactory = rateServiceFactory;
    }


    public async Task<SalaryService> Create(Currency baseCurrency)
    {
        var initialDate = await GetInitialDate();
        var rateService = await _rateServiceFactory.Create(baseCurrency, initialDate);
        
        return new SalaryService(baseCurrency, rateService);


        async Task<DateOnly> GetInitialDate()
        { 
            var minimalDateTime = await _dbContext.Ads
                .OrderBy(ad => ad.Date)
                .Select(ad => ad.Date)
                .FirstOrDefaultAsync();

            return minimalDateTime == default 
                ? DateOnly.FromDateTime(DateTime.Now) 
                : minimalDateTime;
        }
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly RateServiceFactory _rateServiceFactory;
}
