namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// MinHash-based similarity computations.
/// </summary>
public sealed class SimilarityService
{
    /// <summary>
    /// Estimates the Jaccard similarity between two hash signature arrays by computing the proportion of matching
    /// elements at corresponding positions.
    /// </summary>
    /// <remarks>This method is typically used to estimate the similarity between sets represented by MinHash
    /// signatures. The accuracy of the estimate depends on the length of the input arrays.</remarks>
    /// <param name="a">The first array of hash signatures to compare. Must have the same length as <paramref name="b"/> and contain at
    /// least one element.</param>
    /// <param name="b">The second array of hash signatures to compare. Must have the same length as <paramref name="a"/> and contain at
    /// least one element.</param>
    /// <returns>A value between 0.0 and 1.0 representing the estimated Jaccard similarity. Returns 0.0 if either array is empty
    /// or if their lengths differ.</returns>
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
