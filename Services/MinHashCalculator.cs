namespace TgJobAdAnalytics.Services;

public sealed class MinHashCalculator 
{
    public MinHashCalculator(int hashFunctionCount = 100, int seed = 1000)
    {
        _hashFunctionCount = hashFunctionCount;
        _hashFunctions = GenerateHashFunctions(hashFunctionCount, seed);
    }
    
    
    public int[] GenerateSignature(int[] oneHotVector) 
    {
        int vectorLength = oneHotVector.Length;

        var signature = Enumerable.Repeat(int.MaxValue, _hashFunctionCount).ToArray(); 
        for (int i = 0; i < _hashFunctionCount; i++) 
        {
            var hashFunction = _hashFunctions[i];
            for (int j = 0; j < vectorLength; j++) 
            {
                ref int segment = ref signature[i];
                int hashValue = hashFunction(oneHotVector[j]); 
                if (hashValue < segment) 
                    segment = hashValue; 
            }
        }

        return signature; 
    }
    
    
    private static List<Func<int, int>> GenerateHashFunctions(int numHashFunctions, int seed)
    {
        var hashFunctions = new List<Func<int, int>>(numHashFunctions);
        var rand = new Random(seed);

        for (int i = 0; i < numHashFunctions; i++)
        {
            int a = rand.Next();
            int b = rand.Next();
            int c = rand.Next();
            int d = rand.Next();

            hashFunctions.Add(x => HashFunction(x, a, b, c, d));
        }

        return hashFunctions;
    }
    
    
    private static int HashFunction(int value, int a, int b, int c, int d) 
    {
        int hash = a * value.GetHashCode() + b;
        hash ^= hash << c;
        hash ^= hash >> d;

        return Math.Abs(hash);
    }

    
    private readonly int _hashFunctionCount; 
    private readonly List<Func<int, int>> _hashFunctions; 
}
