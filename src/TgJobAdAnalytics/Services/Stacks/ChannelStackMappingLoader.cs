using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Loads channel->stack mapping JSON from disk.
/// </summary>
public sealed class ChannelStackMappingLoader
{
    public ChannelStackMappingLoader(ILogger<ChannelStackMappingLoader> logger, IOptions<StackMappingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }


    public ChannelStackMapping Load()
    {
        var json = File.ReadAllText(_options.MappingFilePath);
        var mapping = JsonSerializer.Deserialize<ChannelStackMapping>(json) ?? new ChannelStackMapping();

        mapping.Channels ??= [];

        return mapping;
    }


    private readonly ILogger<ChannelStackMappingLoader> _logger;
    private readonly StackMappingOptions _options;
}
