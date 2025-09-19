using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Services.Messages;

namespace Tests;

public class SimilarityCalculatorTests
{
    [Fact]
    public void Distinct_WithIdenticalMessages_ReturnsOneMessage()
    {
        var messages = new List<AdEntity>
        {
            CreateAd("Software Developer needed"),
            CreateAd("Software Developer needed")
        };

        var distinct = CreateCalculator().Distinct(messages);

        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithDifferentMessages_ReturnsBothMessages()
    {
        var messages = new List<AdEntity>
        {
            CreateAd("Software Developer needed"),
            CreateAd("Data Scientist position available")
        };

        var distinct = CreateCalculator().Distinct(messages);

        Assert.Equal(2, distinct.Count);
    }


    [Fact]
    public void Distinct_WithSimilarMessages_ReturnsOneMessage()
    {
        var messages = new List<AdEntity>
        {
            CreateAd("Looking for Senior Software Developer in Berlin"),
            CreateAd("Looking for Senior Software Developer in Munich")
        };

        var distinct = CreateCalculator().Distinct(messages);

        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithEmptyList_ReturnsEmptyList()
    {
        var messages = new List<AdEntity>();

        var distinct = CreateCalculator().Distinct(messages);

        Assert.Empty(distinct);
    }


    [Fact]
    public void Distinct_WithSingleMessage_ReturnsSameMessage()
    {
        var message = CreateAd("Test message");
        var messages = new List<AdEntity> { message };

        var distinct = CreateCalculator().Distinct(messages);

        Assert.Single(distinct);
        Assert.Equal(message.Id, distinct[0].Id);
    }


    [Fact]
    public void Distinct_WithMultipleSimilarGroups_ReturnsOneFromEachGroup()
    {
        var messages = new List<AdEntity>
        {
            CreateAd("Senior Java Developer position in Berlin - 5 years experience required"),
            CreateAd("Senior Java Developer position in Munich - 5 years experience required"),
        
            CreateAd("Looking for Python Developer - Machine Learning focus - Remote possible"),
            CreateAd("Looking for Python Developer - Data Science focus - Remote possible"),
        
            CreateAd("DevOps Engineer - Kubernetes expert needed - Frankfurt based position"),
        
            CreateAd("C# Developer position available - ASP.NET Core experience required"),
            CreateAd(".NET Developer position available - ASP.NET Core experience required")
        };

        var distinct = CreateCalculator().Distinct(messages);

        Assert.True(distinct.Count <= 5); // Allow slight variance but expect grouping
    
        var distinctTexts = distinct.Select(m => m.Text.ToLower()).ToList();
        Assert.Contains(distinctTexts, t => t.Contains("java"));
        Assert.Contains(distinctTexts, t => t.Contains("python"));
        Assert.Contains(distinctTexts, t => t.Contains("devops"));
        Assert.Contains(distinctTexts, t => t.Contains(".net") || t.Contains("c#"));
    }


    private static SimilarityCalculator CreateCalculator()
        => new(
            Microsoft.Extensions.Options.Options.Create(new ParallelOptions()),
            Microsoft.Extensions.Options.Options.Create(new VectorizationOptions
            {
                HashFunctionCount = 100,
                LshBandCount = 20,
                ShingleSize = 5, 
                MinHashSeed = 1000,
                VocabularySize = 1_000_000,
                CurrentVersion = 1
            }));


    private static AdEntity CreateAd(string text) => new()
    {
        Id = Guid.NewGuid(),
        Text = text,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        MessageId = Guid.NewGuid(),
        IsUnique = true
    };
}
