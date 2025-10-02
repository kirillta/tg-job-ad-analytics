using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Initializes ChannelStackResolver instances.
/// </summary>
public sealed class ChannelStackResolverFactory
{
    public ChannelStackResolverFactory(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, ChannelStackMappingManager loader)
    {
        _dbContext = dbContext;
        _loader = loader;
        _loggerFactory = loggerFactory;
    }


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
