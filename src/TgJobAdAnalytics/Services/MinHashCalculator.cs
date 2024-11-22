using System.Diagnostics;

namespace TgJobAdAnalytics.Services;

public sealed class MinHashCalculator 
{
    public MinHashCalculator(int hashFunctionCount, int vocabularySize, int seed = 1000)
    {
        Debug.Assert(hashFunctionCount > 0, "Hash function count must be positive");
        Debug.Assert(vocabularySize > 0, "Vocabulary size must be positive");

        _hashFunctionCount = hashFunctionCount;
        
        var universeBitSize = BitsForUniverse(vocabularySize);
        _hashFunctions = GenerateHashFunctions(hashFunctionCount, universeBitSize, seed);

    }
    
    
    public ReadOnlySpan<uint> GenerateSignature(HashSet<string> shingles) 
    {
        var signature = new uint[_hashFunctionCount];
        Array.Fill(signature, uint.MaxValue);
    
        foreach (string shingle in shingles)
        {
            var shingleHash = shingle.GetHashCode();
            for (int i = 0; i < _hashFunctionCount; i++)
            {
                var hashValue = _hashFunctions[i](shingleHash);
                if (hashValue < signature[i])
                    signature[i] = hashValue;
            }
        }
    
        return signature;
    }


    public int HashFunctionCount
        => _hashFunctionCount;


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

            var bMax = 1 << (32 - universeBitSize);
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
        
        return hash >> (32 - universeBitSize);
    }
    

    private readonly int _hashFunctionCount; 
    private readonly List<Func<int, uint>> _hashFunctions; 
}
