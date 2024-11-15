namespace TgJobAdAnalytics.Services;

public sealed class MinHashCalculator
{
    public MinHashCalculator(int hashFunctionsCount)
    {
        _hashFunctionsCount = hashFunctionsCount;
        _hashFunctions = GenerateHashFunctions(hashFunctionsCount);
    }


    public int[] ComputeHash(string text)
    {
        var shingles = GetShingles(text);
        var signature = new int[_hashFunctionsCount];
        Array.Fill(signature, int.MaxValue);

        foreach (var shingle in shingles)
        {
            for (int i = 0; i < _hashFunctionsCount; i++)
            {
                var hashValue = _hashFunctions[i](shingle);
                if (hashValue < signature[i])
                    signature[i] = hashValue;
            }
        }

        return signature;
    }


    private static List<ReadOnlyMemory<char>> GetShingles(string text, int shingleSize = 3)
    {
        var shingles = new List<ReadOnlyMemory<char>>();
        for (int i = 0; i <= text.Length - shingleSize; i++)
            shingles.Add(text.Substring(i, shingleSize).AsMemory());
        
        return shingles;
    }


    private static List<Func<ReadOnlyMemory<char>, int>> GenerateHashFunctions(int numHashFunctions)
    {
        var hashFunctions = new List<Func<ReadOnlyMemory<char>, int>>();
        var random = new Random();

        for (int i = 0; i < numHashFunctions; i++)
        {
            int a = random.Next();
            int b = random.Next();
            int c = random.Next();

            hashFunctions.Add(x => (a * x.GetHashCode() + b) % c);
        }

        return hashFunctions;
    }


    private readonly int _hashFunctionsCount;
    private readonly List<Func<ReadOnlyMemory<char>, int>> _hashFunctions;
}
