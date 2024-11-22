using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgJobAdAnalytics.Models.Analytics;

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
