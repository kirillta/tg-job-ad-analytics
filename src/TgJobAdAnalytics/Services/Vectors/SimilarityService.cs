namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// MinHash-based similarity computations.
/// </summary>
public sealed class SimilarityService : ISimilarityService
{
    public double EstimatedJaccard(uint[] a, uint[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0.0;
        int eq = 0;
        for (int i = 0; i < a.Length; i++) if (a[i] == b[i]) eq++;
        return (double)eq / a.Length;
    }
}
