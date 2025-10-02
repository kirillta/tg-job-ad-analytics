using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Stacks;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Loads channel->stack mapping JSON from disk.
/// </summary>
public sealed class ChannelStackMappingManager
{
    public ChannelStackMappingManager(ILogger<ChannelStackMappingManager> logger, ApplicationDbContext dbContext, IOptions<StackMappingOptions> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
    }


    public async ValueTask<ChannelStackMapping> Update()
    {
        var mapping = UploadFromFile();
        await EnsureTableIsUpToDate(mapping);

        return mapping;
    }


    private async ValueTask EnsureTableIsUpToDate(ChannelStackMapping mapping)
    {
        var existingStackNames = await _dbContext.TechnologyStacks
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

        await UpdateDatabaseWithMissingStacks(missingChannels);
    }


    private async Task UpdateDatabaseWithMissingStacks(List<string> missingChannels)
    {
        _dbContext.TechnologyStacks.AddRange(missingChannels.Select(name => new TechnologyStackEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim()
        }));

        await _dbContext.SaveChangesAsync();
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


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChannelStackMappingManager> _logger;
    private readonly StackMappingOptions _options;
}
