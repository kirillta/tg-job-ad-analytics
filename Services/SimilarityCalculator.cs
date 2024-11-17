using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services;

public sealed class SimilarityCalculator
{
    public static List<Message> Distinct(List<Message> messages)
    {
        var vacabulary = new HashSet<ReadOnlyMemory<char>>();
        var adShingles = new List<HashSet<ReadOnlyMemory<char>>>();

        foreach (var message in messages)
        {
            var shingles = GetShingles(message.Text.AsMemory());
            adShingles.Add(shingles);
            vacabulary.UnionWith(shingles);
        }

        var minHashes = new List<int[]>();
        foreach (var message in messages)
        {
            var vector = ArrayPool<int>.Shared.Rent(vacabulary.Count);

            OneHotEncode(adShingles[messages.IndexOf(message)], vacabulary.ToList(), ref vector);
            var minHash = CalculateMinHash(vector);
            minHashes.Add(minHash);
            
            Array.Clear(vector, 0, vector.Length);
            ArrayPool<int>.Shared.Return(vector);
        }

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


    private static HashSet<ReadOnlyMemory<char>> GetShingles(ReadOnlyMemory<char> text, int shingleSize = 5)
    {
        if (text.Length < shingleSize)
            return Enumerable.Empty<ReadOnlyMemory<char>>().ToHashSet();

        var shingles = new HashSet<ReadOnlyMemory<char>>();
        for (int i = 0; i <= text.Length - shingleSize; i++)
            shingles.Add(text.Slice(i, shingleSize));

        return shingles;
    }


    private static void OneHotEncode(HashSet<ReadOnlyMemory<char>> textShingles, List<ReadOnlyMemory<char>> vocabulary, ref int[] oneHotVector)
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
