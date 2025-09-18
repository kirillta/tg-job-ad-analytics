using TgJobAdAnalytics.Services;

namespace Tests;

public class LocalitySensitiveHashCalculatorTests
{
    [Fact]
    public void Add_StoresSignatureCorrectly()
    {
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        var signature = new uint[] { 1, 2, 3 };

        calculator.Add(1, signature);
        var result = calculator.GetMatches(signature);

        Assert.Contains(1, result);
    }


    [Fact]
    public void Query_WithEmptyCalculator_ReturnsEmptyList()
    {
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        var signature = new uint[] { 1, 2, 3 };

        var result = calculator.GetMatches(signature);

        Assert.Empty(result);
    }


    [Fact]
    public void Query_WithSimilarSignatures_FindsMatches()
    {
        var signature1 = new uint[100];
        var signature2 = new uint[100];

        for (int i = 0; i < 100; i++)
        {
            signature1[i] = (uint)i;

            if (i < 80)
                signature2[i] = (uint)i; // 80% similar
            else
                signature2[i] = (uint)(i + 1000);
        }
        
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        calculator.Add(1, signature1);
        var result = calculator.GetMatches(signature2);

        Assert.Contains(1, result);
    }


    [Fact]
    public void Query_WithDissimilarSignatures_ReturnsEmpty()
    {
        var signature1 = new uint[100];
        var signature2 = new uint[100];

        for (int i = 0; i < 100; i++)
        {
            signature1[i] = (uint)i;
            signature2[i] = (uint)(i + 1000); // Completely different values
        }
        
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        calculator.Add(1, signature1);
        var result = calculator.GetMatches(signature2);

        Assert.Empty(result);
    }


    [Fact]
    public void Add_MultipleSimilarSignatures_GroupsTogether()
    {
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
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


    [Fact]
    public void Query_WithInvalidSignatures_ReturnsEmpty()
    {

        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        var matches = calculator.GetMatches([]);

        Assert.Empty(matches);
    }


    [Fact]
    public void Add_DuplicateId_ThrowsException()
    {
        var calculator = new LocalitySensitiveHashWrapper(100, 20);
        var signature1 = new uint[] { 1, 2, 3 };
        var signature2 = new uint[] { 4, 5, 6 };

        calculator.Add(1, signature1);
        Assert.Throws<Exception>(() => calculator.Add(1, signature2));
    }
}
