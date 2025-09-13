namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Provides access to the active vectorization model parameters.
/// </summary>
public interface IVectorizationConfig
{
    VectorizationModelParams GetActive();
}
