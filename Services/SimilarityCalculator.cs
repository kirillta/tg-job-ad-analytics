using System.Buffers;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services;

public sealed class SimilarityCalculator
{
    public static List<Message> Distinct(List<Message> messages)
    {
        var vacabulary = new HashSet<string>();
        var adShingles = new Dictionary<Message, HashSet<string>>();

        foreach (var message in messages)
        {
            var shingles = GetShingles(message.Text);
            adShingles.Add(message, shingles);
            vacabulary.UnionWith(shingles);
        }

        var vocabularyList = vacabulary.ToList();

        var minHashCalculator = new MinHashCalculator();
        var minHashes = new ConcurrentBag<int[]>();
        Parallel.ForEach(messages, message =>
        {
            var vector = ArrayPool<int>.Shared.Rent(vacabulary.Count);
            OneHotEncode(adShingles[message], vocabularyList, ref vector);
            var hash = minHashCalculator.GenerateSignature(vector);
            minHashes.Add(hash);

            Array.Clear(vector, 0, vector.Length);
            ArrayPool<int>.Shared.Return(vector);
        });

        // Further processing to determine distinct messages based on minHashes

        return messages;
    }


    private static int[] CalculateMinHash(int[] oneHotVector, int numHashFunctions = 100)
    {
        var minHash = new int[numHashFunctions];
        var random = new Random();

        for (int i = 0; i < numHashFunctions; i++)
        {
            minHash[i] = int.MaxValue;
            for (int j = 0; j < oneHotVector.Length; j++)
            {
                if (oneHotVector[j] == 1)
                {
                    var hashValue = random.Next();
                    if (hashValue < minHash[i])
                    {
                        minHash[i] = hashValue;
                    }
                }
            }
        }

        return minHash;
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


    private static void OneHotEncode(HashSet<string> textShingles, List<string> vocabulary, ref int[] oneHotVector)
    {
        for (int i = 0; i < vocabulary.Count; i++)
        {
            if (textShingles.Contains(vocabulary[i]))
                oneHotVector[i] = 1;
            else
                oneHotVector[i] = 0;
        }
    }
}
