using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Vectors;

namespace Tests;

public class VectorizationModelParamsTests
{
    [Fact]
    public void FromOptions_MapsFieldsCorrectly()
    {
        var options = new VectorizationOptions
        {
            CurrentVersion = 2,
            ShingleSize = 7,
            HashFunctionCount = 120,
            MinHashSeed = 42,
            LshBandCount = 24,
            VocabularySize = 500_000
        };

        var p = VectorizationModelParams.FromOptions(options);

        Assert.Equal(2, p.Version);
        Assert.Equal(7, p.ShingleSize);
        Assert.Equal(120, p.HashFunctionCount);
        Assert.Equal(42, p.MinHashSeed);
        Assert.Equal(24, p.LshBandCount);
        Assert.Equal(500_000, p.VocabularySize);
        Assert.Equal(5, p.RowsPerBand); // 120 / 24
    }


    [Fact]
    public void FromOptions_InvalidVersion_DefaultsToOne()
    {
        var options = new VectorizationOptions
        {
            CurrentVersion = 0,
            HashFunctionCount = 100,
            LshBandCount = 20
        };

        var p = VectorizationModelParams.FromOptions(options);
        Assert.Equal(1, p.Version);
    }
}
