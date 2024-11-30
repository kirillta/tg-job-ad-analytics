namespace TgJobAdAnalytics.Models.Messages;

public readonly record struct MessageContainer
{
    public MessageContainer(Message message, Dictionary<string, int> termFrequency)
    {
        Message = message;
        TermFrequency = termFrequency;
        TfIdf = [];
    }


    public Message Message { get; }
    public Dictionary<string, int> TermFrequency { get; }

    public Dictionary<string, double> TfIdf { get; init; }
}
