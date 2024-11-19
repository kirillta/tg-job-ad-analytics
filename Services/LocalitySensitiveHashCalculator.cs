namespace TgJobAdAnalytics.Services;

public sealed class LocalitySensitiveHashCalculator
{
    public LocalitySensitiveHashCalculator(int hashTableCount = 20)
    {
        _hashTableCount = hashTableCount;
        
        _hashTables = new List<Dictionary<uint, List<long>>>(_hashTableCount);
        for (int i = 0; i < _hashTableCount; i++)
            _hashTables.Add([]);
    }
    

    public void Add(long itemId, uint[] signature)
    {
        for (int i = 0; i < _hashTableCount; i++)
        {
            uint hash = ComputeHash(signature, i);
            if (!_hashTables[i].ContainsKey(hash)) 
                _hashTables[i][hash] = [];

            _hashTables[i][hash].Add(itemId);
        }
    }
    
    
    public List<long> Query(uint[] signature)
    {
        var candidates = new HashSet<long>();
        for (int i = 0; i < _hashTableCount; i++)
        {
            uint hash = ComputeHash(signature, i); 
            if (_hashTables[i].TryGetValue(hash, out List<long>? value))
                candidates.UnionWith(value);
        }
        
        return [.. candidates]; 
    }
    
    
    private static uint ComputeHash(uint[] signature, int tableIndex)
    {
        uint hash = 0;
        for (int i = 0; i < signature.Length; i++)
            hash ^= signature[i] + 31 * (uint) tableIndex;
        
        return hash;
    }
    
    
    private readonly int _hashTableCount;
    private readonly List<Dictionary<uint, List<long>>> _hashTables;
}
