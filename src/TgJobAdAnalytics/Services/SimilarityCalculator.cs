using System.Collections.Concurrent;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services;

public sealed class SimilarityCalculator
{
    public static List<Message> Distinct(List<Message> messages)
    {
        if (messages.Count == 0)
            return [];

        var vacabulary = new HashSet<string>();
        var messageShingles = new Dictionary<Message, HashSet<string>>();

        foreach (var message in messages)
        {
            var shingles = GetShingles(message.Text);
            messageShingles.Add(message, shingles);
            vacabulary.UnionWith(shingles);
        }

        var vocabularyList = vacabulary.ToList();

        var minHashCalculator = new MinHashCalculator(100, vocabularyList.Count);
        var lshCalculator = new LocalitySensitiveHashCalculator(minHashCalculator.HashFunctionCount);
        
        var distinctMessages = new ConcurrentBag<Message>();
        Parallel.ForEach(messages, message =>
        {
            var hash = minHashCalculator.GenerateSignature(messageShingles[message]);

            var similarMessages = lshCalculator.GetMatches(hash);
            if (similarMessages.Count == 0)
            {
                lshCalculator.Add(message.Id, hash);
                distinctMessages.Add(message);
            }
        });

        return [.. distinctMessages];
    }


    private static HashSet<string> GetShingles(string text, int shingleSize = 5)
    {
        if (text.Length < shingleSize)
            return Enumerable.Empty<string>().ToHashSet();

        var shingles = new HashSet<string>();
        for (int i = 0; i <= text.Length - shingleSize; i++)
            shingles.Add(text.Substring(i, shingleSize));

        return shingles;
    }
}
