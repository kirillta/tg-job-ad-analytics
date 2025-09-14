using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryPersistenceService
{
    public SalaryPersistenceService(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, SalaryProcessingServiceFactory salaryProcessingServiceFactory)
    {
        _dbContext = dbContext;
        _logger = loggerFactory.CreateLogger<SalaryPersistenceService>();
        _salaryProcessingServiceFactory = salaryProcessingServiceFactory;
    }


    public async Task Process(SalaryEntity? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        try
        {
            if (entity.Period is null || entity.Period is Period.Unknown || entity.Period is Period.Project)
                return;

            var salaryService = await _salaryProcessingServiceFactory.Create(_baseCurrency, cancellationToken);
            var entry = salaryService.Process(entity);
            if (entry is null)
                return;

            _dbContext.Salaries.Update(entry);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting salary for ad {AdId}: {Message}", entity.AdId, ex.Message);
        }
    }


    private readonly Currency _baseCurrency = Currency.RUB;

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryPersistenceService> _logger;
    private readonly SalaryProcessingServiceFactory _salaryProcessingServiceFactory;
}