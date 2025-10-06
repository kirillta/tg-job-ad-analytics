using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryPersistenceService
{
    public SalaryPersistenceService(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, SalaryProcessingServiceFactory salaryProcessingServiceFactory)
    {
        _dbContext = dbContext;
        _logger = loggerFactory.CreateLogger<SalaryPersistenceService>();
        _salaryProcessingServiceFactory = salaryProcessingServiceFactory;
    }


    public async Task Initialize(CancellationToken cancellationToken)
    {
        _salaryProcessingService = await _salaryProcessingServiceFactory.Create(_baseCurrency, cancellationToken);
        _logger.LogInformation("Salary persistence service initialized with base currency: {BaseCurrency}", _baseCurrency);
    }


    public async Task ProcessBatch(List<SalaryEntity> entities, CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return;

        if (_salaryProcessingService is null)
            throw new InvalidOperationException("SalaryPersistenceService must be initialized before processing entities. Call Initialize() first.");

        foreach (var entity in entities)
        {
            try
            {
                if (entity.Period is null || entity.Period is Period.Unknown || entity.Period is Period.Project)
                { 
                    entity.Status = ProcessingStatus.Skipped;
                    continue;    
                }

                var normalizedEntry = _salaryProcessingService.Process(entity);
                if (normalizedEntry is not null)
                    _dbContext.Entry(entity).CurrentValues.SetValues(normalizedEntry);
                else
                    entity.Status = ProcessingStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting salary batch: {Message}", ex.Message);
                entity.Status = ProcessingStatus.Failed;
            }
        }

        await _dbContext.Salaries.AddRangeAsync(entities, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine();
        _logger.LogInformation("Successfully persisted batch of {Count} salaries", entities.Count);
    }


    private readonly Currency _baseCurrency = Currency.RUB;

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryPersistenceService> _logger;
    private SalaryProcessingService? _salaryProcessingService;
    private readonly SalaryProcessingServiceFactory _salaryProcessingServiceFactory;
}