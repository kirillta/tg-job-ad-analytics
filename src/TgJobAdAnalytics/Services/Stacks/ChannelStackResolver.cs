using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// In-memory chat->stack resolver. Must be initialized via Setup before use.
/// Compares by Telegram chat id.
/// </summary>
public sealed class ChannelStackResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelStackResolver"/> class.
    /// </summary>
    public ChannelStackResolver(ILogger<ChannelStackResolver> logger)
    {
        _logger = logger;
    }


    /// <summary>
    /// Sets up the resolver mappings from the provided configuration and canonical stacks.
    /// </summary>
    /// <param name="mapping">Mapping file model.</param>
    /// <param name="stackIdByName">Dictionary of canonical stack name -> id.</param>
    public void Setup(ChannelStackMapping mapping, IReadOnlyDictionary<string, Guid> stackIdByName)
    {
        _stackIdByName = new Dictionary<string, Guid>(stackIdByName, StringComparer.OrdinalIgnoreCase);

        _mapping = mapping.Channels
            .ToDictionary(e => e.ChatId, e => e.StackName.Trim().ToLowerInvariant());

        _logger.LogInformation("ChannelStackResolver initialized. Channels: {Count}", _mapping.Count);
        _initialized = true;
    }


    /// <summary>
    /// Tries to resolve a stack id by telegram chat id.
    /// </summary>
    /// <param name="chatId">Telegram chat id.</param>
    /// <param name="stackId">Resolved stack id.</param>
    /// <returns>True if resolved; otherwise, false.</returns>
    public bool TryResolve(long chatId, out Guid stackId)
    {
        if (!_initialized)
            throw new InvalidOperationException("ChannelStackResolver is not initialized. Call Setup() first.");

        stackId = Guid.Empty;

        if (!_mapping.TryGetValue(chatId, out var stackName))
            return false;

        return _stackIdByName.TryGetValue(stackName, out stackId);
    }


    /// <summary>
    /// Gets the mapping count.
    /// </summary>
    public int MappingCount 
        => _mapping.Count;

    
    private bool _initialized;


    private readonly ILogger<ChannelStackResolver> _logger;
    private Dictionary<long, string> _mapping = [];
    private Dictionary<string, Guid> _stackIdByName = new(StringComparer.OrdinalIgnoreCase);
}
