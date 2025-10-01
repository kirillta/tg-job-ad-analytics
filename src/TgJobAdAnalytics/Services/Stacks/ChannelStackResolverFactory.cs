using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Initializes ChannelStackResolver instances.
/// </summary>
public sealed class ChannelStackResolverFactory
{
    public ChannelStackResolverFactory(ILogger<ChannelStackResolverFactory> logger, ILoggerFactory loggerFactory, ApplicationDbContext dbContext, ChannelStackMappingLoader loader, ChannelStackMappingValidator validator)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dbContext = dbContext;
        _loader = loader;
        _validator = validator;
    }


    public void Initialize(ChannelStackResolver resolver)
    {
        var mapping = _loader.Load();
        _validator.ValidateOrThrow(mapping);

        var stackIdByName = _dbContext.TechnologyStacks
            .AsNoTracking()
            .ToDictionary(s => s.Name.Trim().ToLowerInvariant(), s => s.Id, StringComparer.OrdinalIgnoreCase);

        resolver.Setup(mapping, stackIdByName);
    }


    private readonly ILogger<ChannelStackResolverFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ApplicationDbContext _dbContext;
    private readonly ChannelStackMappingLoader _loader;
    private readonly ChannelStackMappingValidator _validator;
}
