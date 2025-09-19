using TgJobAdAnalytics.Services.Vectors;

namespace Tests;

public class SimilarityServiceVectorTests
{
    [Fact]
    public void EstimatedJaccard_Identical_ReturnsOne()
    {
        uint[] a = [1,2,3,4];
        uint[] b = [1,2,3,4];
        var score = SimilarityService.EstimatedJaccard(a, b);

        Assert.Equal(1.0, score);
    }


    [Fact]
    public void EstimatedJaccard_CompletelyDifferent_ReturnsZero()
    {
        uint[] a = [1,2,3,4];
        uint[] b = [5,6,7,8];
        var score = SimilarityService.EstimatedJaccard(a, b);

        Assert.Equal(0.0, score);
    }


    [Fact]
    public void EstimatedJaccard_PartialOverlap()
    {
        uint[] a = [1,2,3,4];
        uint[] b = [1,9,3,8];
        var score = SimilarityService.EstimatedJaccard(a, b);

        Assert.Equal(0.5, score);
    }


    [Fact]
    public void EstimatedJaccard_DifferentLengths_ReturnsZero()
    {
        uint[] a = [1,2,3];
        uint[] b = [1,2,3,4];
        var score = SimilarityService.EstimatedJaccard(a, b);

        Assert.Equal(0.0, score);
    }
}
