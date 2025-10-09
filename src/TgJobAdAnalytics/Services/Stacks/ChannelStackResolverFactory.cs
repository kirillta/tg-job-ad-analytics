using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Factory responsible for creating and initializing <see cref="ChannelStackResolver"/> instances.
/// Loads the latest channel?stack mapping via <see cref="ChannelStackMappingManager"/>, resolves canonical stack ids,
/// and returns a ready-to-use resolver.
/// </summary>
public sealed class ChannelStackResolverFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelStackResolverFactory"/>.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create a logger for the resolver.</param>
    /// <param name="dbContext">Database context for reading technology stack records.</param>
    /// <param name="loader">Mapping manager that loads channel?stack configuration.</param>
    public ChannelStackResolverFactory(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, ChannelStackMappingManager loader)
    {
        _dbContext = dbContext;
        _loader = loader;
        _loggerFactory = loggerFactory;
    }


    /// <summary>
    /// Creates a fully initialized <see cref="ChannelStackResolver"/> using the current mapping and stack ids.
    /// </summary>
    /// <returns>Initialized resolver.</returns>
    public async Task<ChannelStackResolver> Create()
    {
        var mapping = await _loader.Update();

        var stackIdByName = _dbContext.TechnologyStacks
            .AsNoTracking()
            .ToDictionary(s => s.Name.Trim().ToLowerInvariant(), s => s.Id, StringComparer.OrdinalIgnoreCase);
        
        var resolver = new ChannelStackResolver(_loggerFactory.CreateLogger<ChannelStackResolver>());
        resolver.Setup(mapping, stackIdByName);

        return resolver;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ChannelStackMappingManager _loader;
    private readonly ILoggerFactory _loggerFactory;
}
