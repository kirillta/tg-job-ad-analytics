using TgJobAdAnalytics.Services;

namespace Tests;

public class MinHashCalculatorTests
{
    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        var calculator = new MinHashCalculator(hashFunctionCount: 10, vocabularySize: 1000);
        Assert.Equal(10, calculator.HashFunctionCount);
    }


    [Theory]
    [InlineData(5, 100)]
    [InlineData(10, 1000)]
    [InlineData(20, 10000)]
    public void HashFunctionCount_ReturnsCorrectValue(int hashFunctionCount, int vocabularySize)
    {
        var calculator = new MinHashCalculator(hashFunctionCount, vocabularySize);
        Assert.Equal(hashFunctionCount, calculator.HashFunctionCount);
    }


    [Fact]
    public void GenerateSignature_EmptyShingles_ReturnsMaxValues()
    {
        var calculator = new MinHashCalculator(hashFunctionCount: 5, vocabularySize: 100);
        var shingles = new HashSet<string>();
        var signature = calculator.GenerateSignature(shingles).ToArray();
        
        Assert.Equal(5, signature.Length);
        Assert.All(signature, value => Assert.Equal(uint.MaxValue, value));
    }


    [Fact]
    public void GenerateSignature_SameShingles_ReturnsSameSignature()
    {
        var calculator = new MinHashCalculator(hashFunctionCount: 10, vocabularySize: 100, seed: 42);
        var shingles1 = new HashSet<string> { "test1", "test2" };
        var shingles2 = new HashSet<string> { "test1", "test2" };

        var signature1 = calculator.GenerateSignature(shingles1).ToArray();
        var signature2 = calculator.GenerateSignature(shingles2).ToArray();

        Assert.Equal(signature1, signature2);
    }


    [Fact]
    public void GenerateSignature_DifferentShingles_ReturnsDifferentSignatures()
    {
        var shingles1 = new HashSet<string> { "test1", "test2" };
        var shingles2 = new HashSet<string> { "test3", "test4" };

        var vocabulary = shingles1.Union(shingles2);

        var calculator = new MinHashCalculator(hashFunctionCount: 10, vocabulary.Count());

        var signature1 = calculator.GenerateSignature(shingles1).ToArray();
        var signature2 = calculator.GenerateSignature(shingles2).ToArray();

        Assert.NotEqual(signature1, signature2);
    }


    [Fact]
    public void GenerateSignature_SimilarShingles_ReturnsSimilarSignatures()
    {
        var shingles1 = new HashSet<string> { "test1", "test2", "test3" };
        var shingles2 = new HashSet<string> { "test1", "test2", "test4" }; // 66% similar

        var vocabulary = shingles1.Union(shingles2);

        var calculator = new MinHashCalculator(hashFunctionCount: 100, vocabulary.Count());

        var signature1 = calculator.GenerateSignature(shingles1).ToArray();
        var signature2 = calculator.GenerateSignature(shingles2).ToArray();

        double matchingHashes = signature1.Zip(signature2, (a, b) => a == b ? 1 : 0).Sum();
        double similarity = matchingHashes / calculator.HashFunctionCount;

        Assert.True(similarity >= 0.5);
    }
}
