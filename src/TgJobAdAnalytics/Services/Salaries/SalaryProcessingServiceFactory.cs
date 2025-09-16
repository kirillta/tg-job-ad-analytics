using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryProcessingServiceFactory
{
    public SalaryProcessingServiceFactory(ApplicationDbContext dbContext, RateServiceFactory rateServiceFactory) 
    {
        _dbContext = dbContext;
        _rateServiceFactory = rateServiceFactory;
    }


    public async Task<SalaryProcessingService> Create(Currency baseCurrency, CancellationToken cancellationToken)
    {
        if (_services.TryGetValue(baseCurrency, out var existingService))
            return existingService;

        var initialDate = await GetInitialDate(cancellationToken);
        var rateService = await _rateServiceFactory.Create(baseCurrency, initialDate, cancellationToken);
        
        var newService = new SalaryProcessingService(baseCurrency, rateService);
        _services[baseCurrency] = newService;
        
        return newService;


        async Task<DateOnly> GetInitialDate(CancellationToken cancellationToken)
        { 
            var minimalDateTime = await _dbContext.Ads
                .OrderBy(ad => ad.Date)
                .Select(ad => ad.Date)
                .FirstOrDefaultAsync(cancellationToken);

            return minimalDateTime == default 
                ? DateOnly.FromDateTime(DateTime.UtcNow) 
                : minimalDateTime;
        }
    }


    private readonly Dictionary<Currency, SalaryProcessingService> _services = [];

    private readonly ApplicationDbContext _dbContext;
    private readonly RateServiceFactory _rateServiceFactory;
}
