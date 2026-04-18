using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Models.Vectors;

/// <summary>
/// Default implementation of IVectorizationConfig reading from VectorizationOptions.
/// </summary>
public sealed class OptionVectorizationConfig
{
    /// <summary>
    /// Initializes a new instance of the OptionVectorizationConfig class using the specified vectorization options.
    /// </summary>
    /// <param name="options">The options wrapper containing the vectorization configuration values to use.</param>
    public OptionVectorizationConfig(IOptions<VectorizationOptions> options)
    {
        _options = options.Value;
    }


    /// <summary>
    /// Gets the currently active vectorization model parameters based on the configured options.
    /// </summary>
    /// <returns>The active <see cref="VectorizationModelParams"/> instance derived from the current options.</returns>
    public VectorizationModelParams GetActive() 
        => VectorizationModelParams.FromOptions(_options);


    private readonly VectorizationOptions _options;
}
