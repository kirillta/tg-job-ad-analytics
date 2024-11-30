using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Services.Messages;

namespace Tests;

public class SimilarityCalculatorTests
{
    [Fact]
    public void Distinct_WithIdenticalMessages_ReturnsOneMessage()
    {
        var messages = new List<Message>
        {
            CreateMessage(1, "Software Developer needed"),
            CreateMessage(2, "Software Developer needed")
        };

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithDifferentMessages_ReturnsBothMessages()
    {
        var messages = new List<Message>
        {
            CreateMessage(1, "Software Developer needed"),
            CreateMessage(2, "Data Scientist position available")
        };

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Equal(2, distinct.Count);
    }


    [Fact]
    public void Distinct_WithSimilarMessages_ReturnsOneMessage()
    {
        var messages = new List<Message>
        {
            CreateMessage(1, "Looking for Senior Software Developer in Berlin"),
            CreateMessage(2, "Looking for Senior Software Developer in Munich")
        };

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithEmptyList_ReturnsEmptyList()
    {
        var messages = new List<Message>();

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Empty(distinct);
    }


    [Fact]
    public void Distinct_WithSingleMessage_ReturnsSameMessage()
    {
        var message = CreateMessage(1, "Test message");
        var messages = new List<Message> { message };

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Single(distinct);
        Assert.Equal(message.Id, distinct[0].Id);
    }


    [Fact]
    public void Distinct_WithMultipleSimilarGroups_ReturnsOneFromEachGroup()
    {
        var messages = new List<Message>
        {
            CreateMessage(1, "Senior Java Developer position in Berlin - 5 years experience required"),
            CreateMessage(2, "Senior Java Developer position in Munich - 5 years experience required"),
        
            CreateMessage(3, "Looking for Python Developer - Machine Learning focus - Remote possible"),
            CreateMessage(4, "Looking for Python Developer - Data Science focus - Remote possible"),
        
            CreateMessage(5, "DevOps Engineer - Kubernetes expert needed - Frankfurt based position"),
        
            CreateMessage(6, "C# Developer position available - ASP.NET Core experience required"),
            CreateMessage(7, ".NET Developer position available - ASP.NET Core experience required")
        };

        var distinct = SimilarityCalculator.Distinct(messages);

        Assert.Equal(4, distinct.Count); // One from each group (Java, Python, DevOps, .NET)
    
        var distinctTexts = distinct.Select(m => m.Text.ToLower()).ToList();
        Assert.Contains(distinctTexts, t => t.Contains("java"));
        Assert.Contains(distinctTexts, t => t.Contains("python"));
        Assert.Contains(distinctTexts, t => t.Contains("devops"));
        Assert.Contains(distinctTexts, t => t.Contains(".net") || t.Contains("c#"));
    }


    private static Message CreateMessage(long id, string text) => new() 
    { 
        Id = id, 
        Text = text 
    };
}
