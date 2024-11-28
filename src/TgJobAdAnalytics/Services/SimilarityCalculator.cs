using System.Collections.Concurrent;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services;

public sealed class SimilarityCalculator
{
    public SimilarityCalculator(ParallelOptions parallelOptions)
    {
        _parallelOptions = parallelOptions;
    }


    public List<Message> Distinct(List<Message> messages)
    {
        if (messages.Count == 0)
            return [];

        var (shingles, vocabulary) = GetShinglesAndVocabulary(messages);

        return DistinctInternal(messages, shingles);


        static HashSet<string> GetShingles(string text, int shingleSize = 5)
        {
            if (text.Length < shingleSize)
                return Enumerable.Empty<string>().ToHashSet();

            var shingles = new HashSet<string>();
            for (int i = 0; i <= text.Length - shingleSize; i++)
                shingles.Add(text.Substring(i, shingleSize));

            return shingles;
        }


        (Dictionary<Message, HashSet<string>>, List<string>) GetShinglesAndVocabulary(List<Message> messages)
        {
            var vocabulary = new HashSet<string>();
            var messageShingles = new Dictionary<Message, HashSet<string>>();
            foreach (var message in messages)
            {
                var shingles = GetShingles(message.Text);
                messageShingles.Add(message, shingles);
                vocabulary.UnionWith(shingles);
            }

            return (messageShingles, vocabulary.ToList());
        }


        List<Message> DistinctInternal(List<Message> messages, Dictionary<Message, HashSet<string>> shingles)
        {
            var minHashCalculator = new MinHashCalculator(100, vocabulary.Count);
            var lshCalculator = new LocalitySensitiveHashCalculator(minHashCalculator.HashFunctionCount);

            var distinctMessages = new ConcurrentBag<Message>();
            Parallel.ForEach(messages, _parallelOptions, message =>
            {
                var hash = minHashCalculator.GenerateSignature(shingles[message]);

                var similarMessages = lshCalculator.GetMatches(hash);
                if (similarMessages.Count == 0)
                {
                    lshCalculator.Add(message.Id, hash);
                    distinctMessages.Add(message);
                }
            });

            return [.. distinctMessages];
        }
    }


    private readonly ParallelOptions _parallelOptions;
}
