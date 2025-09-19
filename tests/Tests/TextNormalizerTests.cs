using TgJobAdAnalytics.Services.Messages;

namespace Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("Hello   world", "Hello world")] // multiple spaces compressed
    [InlineData("Hello , world", "Hello world")] // space before comma removed
    [InlineData("100 000 usd", "100000$")] // thousand separator removed + currency replaced
    [InlineData("100k usd", "100k $")] // currency name to symbol
    [InlineData("100k- 200k usd", "100k-200k $")] // spaces around dash removed
    [InlineData("100k -200k usd", "100k -200k $")] // spaces around dash removed
    [InlineData("100k - 200k usd", "100k -200k $")] // spaces around dash removed
    [InlineData("100k ?.", "100k")] // ruble short form
    [InlineData("100k EUR", "100k €")] // euro replacement
    public void NormalizeTextEntry_TransformsCorrectly(string input, string expected)
    {
        var normalized = TextNormalizer.NormalizeTextEntry(input);
        Assert.Equal(expected, normalized);
    }


    [Fact]
    public void NormalizeTextEntry_ReplacesEmAndEnDash()
    {
        var input = "100k — 200k usd"; // em dash
        var normalized = TextNormalizer.NormalizeTextEntry(input);
        Assert.Equal("100k -200k $", normalized);

        input = "100k – 200k usd"; // en dash
        normalized = TextNormalizer.NormalizeTextEntry(input);
        Assert.Equal("100k -200k $", normalized);
    }


    [Theory]
    [InlineData("100 $", "100$")]
    [InlineData("200 €", "200€")]
    public void NormalizeTextEntry_RemovesSpaceBeforeCurrency(string input, string expected)
    {
        var normalized = TextNormalizer.NormalizeTextEntry(input);

        Assert.Equal(expected, normalized);
    }


    [Fact]
    public void NormalizeAdText_RemovesSpaceBeforeSeparators()
    {
        var input = "Hello , world : welcome ; now";
        var normalized = TextNormalizer.NormalizeAdText(input);

        Assert.Equal("Hello world welcome now", normalized);
    }


    [Fact]
    public void NormalizeTextEntry_EmptyReturnsEmpty()
    {
        var input = "";
        var normalized = TextNormalizer.NormalizeAdText(input);

        Assert.Equal("", normalized);
    }
}
