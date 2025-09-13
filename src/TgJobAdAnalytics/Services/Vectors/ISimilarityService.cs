namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Computes similarity scores between a query signature and candidate signatures.
/// </summary>
public interface ISimilarityService
{
    double EstimatedJaccard(uint[] a, uint[] b);
}
