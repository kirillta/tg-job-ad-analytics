using System.Diagnostics;
using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Services.Messages;

public sealed class MinHashCalculator
{
    public MinHashCalculator(VectorizationOptions vectorizationOptions, int vocabularySize)
    {
        Debug.Assert(vectorizationOptions.HashFunctionCount > 0, "Hash function count must be positive");
        Debug.Assert(vocabularySize > 0, "Vocabulary size must be positive");
        
        _vectorizationOptions = vectorizationOptions;

        var universeBitSize = BitsForUniverse(vocabularySize);
        _hashFunctions = GenerateHashFunctions(_vectorizationOptions.HashFunctionCount, universeBitSize, _vectorizationOptions.MinHashSeed);
    }


    public ReadOnlySpan<uint> GenerateSignature(HashSet<string> shingles)
    {
        var signature = new uint[_vectorizationOptions.HashFunctionCount];
        Array.Fill(signature, uint.MaxValue);

        foreach (string shingle in shingles)
        {
            var shingleHash = shingle.GetHashCode();
            for (int i = 0; i < _vectorizationOptions.HashFunctionCount; i++)
            {
                var hashValue = _hashFunctions[i](shingleHash);
                if (hashValue < signature[i])
                    signature[i] = hashValue;
            }
        }

        return signature;
    }


    private static int BitsForUniverse(int universeSize)
        => (int)Math.Truncate(Math.Log(universeSize, 2.0)) + 1;


    private static List<Func<int, uint>> GenerateHashFunctions(int numHashFunctions, int universeBitSize, int seed)
    {
        var hashFunctions = new List<Func<int, uint>>(numHashFunctions);
        var rand = new Random(seed);

        for (int i = 0; i < numHashFunctions; i++)
        {
            uint a = 0;
            while (a % 2 == 0 || a <= 0)
                a = (uint)rand.Next();

            var bMax = 1 << 32 - universeBitSize;
            uint b = 0;
            while (b <= 0)
                b = (uint)rand.Next(bMax);

            hashFunctions.Add(x => HashFunction(x, a, b, universeBitSize));
        }

        return hashFunctions;
    }


    // FNV-1a hash
    private static uint HashFunction(int value, uint a, uint b, int universeBitSize)
    {
        const uint FNV_PRIME = 16777619;
        const uint FNV_OFFSET = 2166136261;

        uint hash = FNV_OFFSET;
        hash ^= (uint)value;
        hash *= FNV_PRIME;
        hash ^= a;
        hash *= FNV_PRIME;
        hash ^= b;

        return hash >> 32 - universeBitSize;
    }


    private readonly List<Func<int, uint>> _hashFunctions;
    private readonly VectorizationOptions _vectorizationOptions;
}
