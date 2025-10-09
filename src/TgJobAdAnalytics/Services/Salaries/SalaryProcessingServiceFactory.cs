using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Factory that creates and caches <see cref="SalaryProcessingService"/> instances per base currency.
/// Resolves the initial date (oldest ad date) to seed underlying rate services so normalization covers the full dataset.
/// Subsequent calls for the same currency return the cached instance.
/// </summary>
public sealed class SalaryProcessingServiceFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryProcessingServiceFactory"/>.
    /// </summary>
    /// <param name="dbContext">Application database context used to determine earliest advertisement date.</param>
    /// <param name="rateServiceFactory">Factory for creating currency rate services.</param>
    public SalaryProcessingServiceFactory(ApplicationDbContext dbContext, RateServiceFactory rateServiceFactory) 
    {
        _dbContext = dbContext;
        _rateServiceFactory = rateServiceFactory;
    }


    /// <summary>
    /// Returns a <see cref="SalaryProcessingService"/> for the specified base currency, creating and caching it on first use.
    /// The earliest ad date is used to determine the historical range of exchange rates to preload.
    /// </summary>
    /// <param name="baseCurrency">Base currency for normalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Initialized <see cref="SalaryProcessingService"/> instance.</returns>
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
