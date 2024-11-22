namespace TgJobAdAnalytics.Services;

public sealed class MinHashCalculator 
{
    public MinHashCalculator(int hashFunctionCount, int vocabularySize, int seed = 1000)
    {
        _hashFunctionCount = hashFunctionCount;
        
        var universeBitSize = BitsForUniverse(vocabularySize);
        _hashFunctions = GenerateHashFunctions(hashFunctionCount, universeBitSize, seed);

    }
    
    
    public ReadOnlySpan<uint> GenerateSignature(int[] oneHotVector) 
    {
        int vectorLength = oneHotVector.Length;

        var signature = Enumerable.Repeat(uint.MaxValue, _hashFunctionCount).ToArray();
        for (int i = 0; i < vectorLength; i++)
        {
            if (oneHotVector[i] == 0)
                continue;

            for (int j = 0; j < _hashFunctionCount; j++)
            {
                ref uint segment = ref signature[j];
                var hashValue = _hashFunctions[j](oneHotVector[i]);
                if (hashValue < segment)
                    segment = hashValue;
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
    
    
    private static uint HashFunction(int value, uint a, uint b, int universeBitSize) 
    {
        // FNV-1a hash
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
