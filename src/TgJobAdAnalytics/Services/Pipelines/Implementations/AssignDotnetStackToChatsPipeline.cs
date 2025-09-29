using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Pipelines.Implementations;

/// <summary>
/// Assigns the 'dotnet' technology stack to all ads belonging to existing chats where the stack is missing.
/// </summary>
public sealed class AssignDotnetStackToChatsPipeline : IPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignDotnetStackToChatsPipeline"/> class.
    /// </summary>
    public AssignDotnetStackToChatsPipeline(ILoggerFactory loggerFactory, ApplicationDbContext dbContext)
    {
        _logger = loggerFactory.CreateLogger<AssignDotnetStackToChatsPipeline>();
        _dbContext = dbContext;
    }


    /// <inheritdoc/>
    public string Name 
        => "assign-dotnet-to-chats";


    /// <inheritdoc/>
    public string Description 
        => "Assign 'dotnet' stack to all ads for existing chats where StackId is null.";


    /// <inheritdoc/>
    public bool IsIdempotent 
        => true;


    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var dotnetStackId = await _dbContext.TechnologyStacks
            .AsNoTracking()
            .Where(s => s.Name.Equals("dotnet", StringComparison.CurrentCultureIgnoreCase))
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (dotnetStackId == Guid.Empty)
            throw new InvalidOperationException("Technology stack 'dotnet' not found. Seed it first.");

        var chatIds = await _dbContext.Chats
            .AsNoTracking()
            .Select(c => c.TelegramId)
            .ToListAsync(cancellationToken);

        if (chatIds.Count == 0)
            return 0;

        var targetAds = _dbContext.Ads
            .IgnoreQueryFilters()
            .Where(a => a.StackId == null)
            .Join(_dbContext.Messages.AsNoTracking(), a => a.MessageId, m => m.Id, (a, m) => new { a, m.TelegramChatId })
            .Where(x => chatIds.Contains(x.TelegramChatId))
            .Select(x => x.a);

        var updated = await targetAds.ExecuteUpdateAsync(
            setters => setters.SetProperty(a => a.StackId, dotnetStackId),
            cancellationToken);

        _logger.LogInformation("AssignDotnetStackToChats: updated {Count} ads", updated);
        return updated;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AssignDotnetStackToChatsPipeline> _logger;
}
