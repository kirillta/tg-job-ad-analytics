using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Default implementation of IVectorizationConfig reading from VectorizationOptions.
/// </summary>
public sealed class OptionVectorizationConfig : IVectorizationConfig
{
    public OptionVectorizationConfig(IOptions<VectorizationOptions> options)
    {
        _options = options.Value;
    }

    public VectorizationModelParams GetActive() => VectorizationModelParams.FromOptions(_options);

    private readonly VectorizationOptions _options;
}
