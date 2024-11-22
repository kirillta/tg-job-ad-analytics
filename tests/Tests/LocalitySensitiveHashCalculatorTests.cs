using TgJobAdAnalytics.Services;

namespace Tests;

public class LocalitySensitiveHashCalculatorTests
{
    [Fact]
    public void Add_StoresSignatureCorrectly()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var signature = new uint[] { 1, 2, 3 };

        calculator.Add(1, signature);
        var result = calculator.GetMatches(signature);

        Assert.Contains(1, result);
    }


    [Fact]
    public void Query_WithEmptyCalculator_ReturnsEmptyList()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var signature = new uint[] { 1, 2, 3 };

        var result = calculator.GetMatches(signature);

        Assert.Empty(result);
    }


    [Fact]
    public void Query_WithSimilarSignatures_FindsMatches()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var signature1 = new uint[] { 1, 2, 3, 4, 5 };
        var signature2 = new uint[] { 1, 2, 3, 4, 6 }; // 80% similar

        calculator.Add(1, signature1);
        var result = calculator.GetMatches(signature2);

        Assert.Contains(1, result);
    }


    [Fact]
    public void Query_WithDissimilarSignatures_ReturnsEmpty()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var signature1 = new uint[] { 1, 2, 3, 4, 5 };
        var signature2 = new uint[] { 6, 7, 8, 9, 10 }; // 0% similar

        calculator.Add(1, signature1);
        var result = calculator.GetMatches(signature2);

        Assert.Empty(result);
    }


    [Fact]
    public void Add_MultipleSimilarSignatures_GroupsTogether()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var baseSignature = new uint[] { 1, 2, 3, 4, 5 };
        var similarSignature1 = new uint[] { 1, 2, 3, 4, 6 };
        var similarSignature2 = new uint[] { 1, 2, 3, 5, 6 };

        calculator.Add(1, baseSignature);
        calculator.Add(2, similarSignature1);
        calculator.Add(3, similarSignature2);

        var matches = calculator.GetMatches(baseSignature);
        Assert.Contains(1, matches);
        Assert.Contains(2, matches);
        Assert.Contains(3, matches);
    }


    [Theory]
    [InlineData(null)]
    [InlineData(new uint[0])]
    public void Query_WithInvalidSignatures_ThrowsArgumentException(uint[]? signature)
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        Assert.Throws<ArgumentException>(() => calculator.GetMatches(signature));
    }


    [Fact]
    public void Add_DuplicateId_UpdatesSignature()
    {
        var calculator = new LocalitySensitiveHashCalculator(100, 20);
        var signature1 = new uint[] { 1, 2, 3 };
        var signature2 = new uint[] { 4, 5, 6 };

        calculator.Add(1, signature1);
        calculator.Add(1, signature2);

        var result = calculator.GetMatches(signature1);
        Assert.DoesNotContain(1, result);

        result = calculator.GetMatches(signature2);
        Assert.Contains(1, result);
    }
}
