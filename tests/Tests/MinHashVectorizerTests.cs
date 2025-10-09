using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Vectors;
using TgJobAdAnalytics.Services.Vectors;

namespace Tests;

public class MinHashVectorizerTests
{
    private static OptionVectorizationConfig CreateConfig(int hashCount = 50, int bandCount = 10, int shingleSize = 3, int seed = 123, int vocab = 10_000)
    {
        var opts = Options.Create(new VectorizationOptions
        {
            HashFunctionCount = hashCount,
            LshBandCount = bandCount,
            ShingleSize = shingleSize,
            MinHashSeed = seed,
            VocabularySize = vocab,
            CurrentVersion = 1
        });

        return new OptionVectorizationConfig(opts);
    }


    [Fact]
    public void Compute_ReturnsExpectedSignatureSize()
    {
        var vectorizer = new MinHashVectorizer(CreateConfig(hashCount: 32, bandCount: 8));
        var (sig, count) = vectorizer.GenerateMinHashSignature("abcdefghijk");

        Assert.Equal(32, sig.Length);
        Assert.True(count > 0);
    }


    [Fact]
    public void Compute_SameTextSameSeed_Deterministic()
    {
        var text = "Senior Backend Engineer in Berlin";
        var a = new MinHashVectorizer(CreateConfig(seed: 999));
        var b = new MinHashVectorizer(CreateConfig(seed: 999));

        var (sig1, count1) = a.GenerateMinHashSignature(text);
        var (sig2, count2) = b.GenerateMinHashSignature(text);

        Assert.Equal(count1, count2);
        Assert.Equal(sig1, sig2);
    }


    [Fact]
    public void Compute_DifferentSeed_DifferentSignature()
    {
        var text = "Senior Backend Engineer in Berlin";
        var a = new MinHashVectorizer(CreateConfig(seed: 111));
        var b = new MinHashVectorizer(CreateConfig(seed: 222));

        var (sig1, _) = a.GenerateMinHashSignature(text);
        var (sig2, _) = b.GenerateMinHashSignature(text);

        Assert.NotEqual(sig1, sig2);
    }


    [Fact]
    public void Compute_ShortTextNoShingles_ReturnsEmptySignatureFilled()
    {
        var vectorizer = new MinHashVectorizer(CreateConfig(shingleSize: 50));
        var (sig, count) = vectorizer.GenerateMinHashSignature("short");

        Assert.Equal(0, count);
        Assert.Equal(50, sig.Length); // hash function count default from config
    }
}
