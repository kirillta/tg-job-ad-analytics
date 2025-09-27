using Microsoft.Extensions.Logging;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Validates mapping on startup and fails fast if invalid.
/// </summary>
public sealed class StackMappingStartupValidator
{
    public StackMappingStartupValidator(ILogger<StackMappingStartupValidator> logger, ChannelStackMappingLoader loader, ChannelStackMappingValidator validator)
    {
        _logger = logger;
        _loader = loader;
        _validator = validator;
    }


    public void ValidateOrThrow()
    {
        var mapping = _loader.Load();
        _validator.ValidateOrThrow(mapping);

        _logger.LogInformation("Stack mapping validation passed. Channels: {Count}", mapping.Channels.Count);
    }


    private readonly ILogger<StackMappingStartupValidator> _logger;
    private readonly ChannelStackMappingLoader _loader;
    private readonly ChannelStackMappingValidator _validator;
}
