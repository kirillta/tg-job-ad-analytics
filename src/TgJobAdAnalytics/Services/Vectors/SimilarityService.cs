namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// MinHash-based similarity computations.
/// </summary>
public sealed class SimilarityService
{
    public static double EstimatedJaccard(uint[] a, uint[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) 
            return 0.0;

        var equalCount = 0;
        for (var i = 0; i < a.Length; i++) 
        {
            if (a[i] == b[i]) 
                equalCount++;
        }

        return (double)equalCount / a.Length;
    }
}
