using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Handles persistence of extracted salary entities including post-extraction processing / normalization
/// (currency conversion, monthly conversion, boundary validation). Must be initialized before processing
/// to load the appropriate <see cref="SalaryProcessingService"/> for the configured base currency.
/// </summary>
public sealed class SalaryPersistenceService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryPersistenceService"/>.
    /// </summary>
    /// <param name="loggerFactory">Logger factory to create the service logger.</param>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="salaryProcessingServiceFactory">Factory for creating salary processing services.</param>
    public SalaryPersistenceService(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, SalaryProcessingServiceFactory salaryProcessingServiceFactory)
    {
        _dbContext = dbContext;
        _logger = loggerFactory.CreateLogger<SalaryPersistenceService>();
        _salaryProcessingServiceFactory = salaryProcessingServiceFactory;
    }


    /// <summary>
    /// Initializes the service by creating (or retrieving) a processing service for the configured base currency.
    /// Must be called prior to <see cref="ProcessBatch"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Initialize(CancellationToken cancellationToken)
    {
        _salaryProcessingService = await _salaryProcessingServiceFactory.Create(_baseCurrency, cancellationToken);
        _logger.LogInformation("Salary persistence service initialized with base currency: {BaseCurrency}", _baseCurrency);
    }


    /// <summary>
    /// Processes and persists a batch of salary entities. Each entity is normalized (converted to monthly/base currency)
    /// unless its period is unknown or project-based, in which case it is skipped. Failed entities are marked accordingly.
    /// </summary>
    /// <param name="entities">Collection of salary entities to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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