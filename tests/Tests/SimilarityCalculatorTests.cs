using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Services;

namespace Tests;

public class SimilarityCalculatorTests
{
    [Fact]
    public void Distinct_WithIdenticalMessages_ReturnsOneMessage()
    {
        // Arrange
        var messages = new List<Message>
        {
            CreateMessage(1, "Software Developer needed"),
            CreateMessage(2, "Software Developer needed")
        };

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithDifferentMessages_ReturnsBothMessages()
    {
        // Arrange
        var messages = new List<Message>
        {
            CreateMessage(1, "Software Developer needed"),
            CreateMessage(2, "Data Scientist position available")
        };

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Equal(2, distinct.Count);
    }


    [Fact]
    public void Distinct_WithSimilarMessages_ReturnsOneMessage()
    {
        // Arrange
        var messages = new List<Message>
        {
            CreateMessage(1, "Looking for Senior Software Developer in Berlin"),
            CreateMessage(2, "Looking for Senior Software Developer in Munich")
        };

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Single(distinct);
    }


    [Fact]
    public void Distinct_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var messages = new List<Message>();

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Empty(distinct);
    }


    [Fact]
    public void Distinct_WithSingleMessage_ReturnsSameMessage()
    {
        // Arrange
        var message = CreateMessage(1, "Test message");
        var messages = new List<Message> { message };

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Single(distinct);
        Assert.Equal(message.Id, distinct[0].Id);
    }


    [Fact]
    public void Distinct_WithMultipleSimilarGroups_ReturnsOneFromEachGroup()
    {
        // Arrange
        var messages = new List<Message>
        {
            CreateMessage(1, "Java Developer position"),
            CreateMessage(2, "Java Developer role"),
            CreateMessage(3, "Python Developer needed"),
            CreateMessage(4, "Python Developer position open"),
            CreateMessage(5, "DevOps Engineer")
        };

        // Act
        var distinct = SimilarityCalculator.Distinct(messages);

        // Assert
        Assert.Equal(3, distinct.Count); // One from each group (Java, Python, DevOps)
    }


    private static Message CreateMessage(long id, string text) => new() 
    { 
        Id = id, 
        Text = text 
    };
}
