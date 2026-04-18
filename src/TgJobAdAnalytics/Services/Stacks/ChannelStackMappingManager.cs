using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Stacks;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Loads channel–technology stack mapping JSON from disk and ensures referenced stacks exist in the database.
/// Adds any missing <see cref="TechnologyStackEntity"/> entries before returning the in-memory mapping model.
/// The mapping is initialized once and cached for the lifetime of the singleton.
/// </summary>
public sealed class ChannelStackMappingManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelStackMappingManager"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="dbContextFactory">Factory used to create short-lived database contexts for initialization.</param>
    /// <param name="options">Options providing the mapping JSON file path.</param>
    public ChannelStackMappingManager(ILogger<ChannelStackMappingManager> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, IOptions<StackMappingOptions> options)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _options = options.Value;
    }


    /// <summary>
    /// Returns the channel–stack mapping, initializing and seeding the database on the first call.
    /// Subsequent calls return the cached result without any database access.
    /// </summary>
    /// <returns>The deserialized <see cref="ChannelStackMapping"/> model.</returns>
    public async ValueTask<ChannelStackMapping> Update()
    {
        if (_cachedMapping is not null)
            return _cachedMapping;

        await _initLock.WaitAsync();
        try
        {
            if (_cachedMapping is not null)
                return _cachedMapping;

            var mapping = UploadFromFile();
            await EnsureTableIsUpToDate(mapping);
            _cachedMapping = mapping;

            return _cachedMapping;
        }
        finally
        {
            _initLock.Release();
        }
    }


    private async ValueTask EnsureTableIsUpToDate(ChannelStackMapping mapping)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var existingStackNames = await dbContext.TechnologyStacks
            .AsNoTracking()
            .Select(s => s.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

        var missingChannels = mapping.Channels
            .Where(c => !existingStackNames.Contains(c.StackName))
            .Select(c => c.StackName.ToLowerInvariant().Normalize())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingChannels.Count == 0)
            return;

        await UpdateDatabaseWithMissingStacks(dbContext, missingChannels);
    }


    private async Task UpdateDatabaseWithMissingStacks(ApplicationDbContext dbContext, List<string> missingChannels)
    {
        dbContext.TechnologyStacks.AddRange(missingChannels.Select(name => new TechnologyStackEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim()
        }));

        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Added {Count} missing technology stacks to the database.", missingChannels.Count);
    }


    private ChannelStackMapping UploadFromFile()
    {
        if (!File.Exists(_options.MappingFilePath))
        {
            _logger.LogWarning("Technology stack source file not found: {MappingFilePath}. Creating a new file.", _options.MappingFilePath);
            throw new FileNotFoundException("Mapping file not found.", _options.MappingFilePath);
        }

        var json = File.ReadAllText(_options.MappingFilePath);
        var mapping = JsonSerializer.Deserialize<ChannelStackMapping>(json) ?? new ChannelStackMapping();

        mapping.Channels ??= [];

        return mapping;
    }


    private ChannelStackMapping? _cachedMapping;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<ChannelStackMappingManager> _logger;
    private readonly StackMappingOptions _options;
}
