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


    public async Task ValidateOrThrow(CancellationToken cancellationToken)
    {
        var mapping = _loader.Load();
        await _validator.ValidateOrThrow(mapping, cancellationToken);

        _logger.LogInformation("Stack mapping validation passed. Channels: {Count}", mapping.Channels.Count);
    }


    private readonly ILogger<StackMappingStartupValidator> _logger;
    private readonly ChannelStackMappingLoader _loader;
    private readonly ChannelStackMappingValidator _validator;
}
