using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// In-memory channel->stack resolver. Must be initialized via Setup before use.
/// </summary>
public sealed class ChannelStackResolver
{
    public ChannelStackResolver(ILogger<ChannelStackResolver> logger)
    {
        _logger = logger;
    }


    public void Setup(ChannelStackMapping mapping, IReadOnlyDictionary<string, Guid> stackIdByName)
    {
        _stackIdByName = new Dictionary<string, Guid>(stackIdByName, StringComparer.OrdinalIgnoreCase);
        _mapping = mapping.Channels
            .ToDictionary(e => Normalize(e.Channel), e => e.Stack.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("ChannelStackResolver initialized. Channels: {Count}", _mapping.Count);
        _initialized = true;
    }


    public bool TryResolve(string channelName, out Guid stackId)
    {
        if (!_initialized)
            throw new InvalidOperationException("ChannelStackResolver is not initialized. Call Setup() first.");

        stackId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(channelName))
            return false;

        var key = Normalize(channelName);
        if (!_mapping.TryGetValue(key, out var stackName))
            return false;

        return _stackIdByName.TryGetValue(stackName, out stackId);
    }


    public int MappingCount => _mapping.Count;


    private static string Normalize(string s)
        => (s ?? string.Empty).Trim().ToLowerInvariant();


    private readonly ILogger<ChannelStackResolver> _logger;
    private Dictionary<string, string> _mapping = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Guid> _stackIdByName = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
}
