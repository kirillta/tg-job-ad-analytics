using TgJobAdAnalytics.Models.Analytics;

namespace TgJobAdAnalytics.Services.Salaries;

public class RateSourceManager
{
    public RateSourceManager(string rateSourcePath)
    {
        _rateSourcePath = rateSourcePath;

        UploadFromFile();
    }


    public async Task<Dictionary<(Currency, Currency, DateOnly), double>> Add(Currency baseCurrency, Currency targetCurrency, DateOnly targetDate, double rate)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var writer = new StreamWriter(_rateSourcePath, true);
            await writer.WriteLineAsync($"{baseCurrency},{targetCurrency},{targetDate},{rate}");
            
            _rates[(baseCurrency, targetCurrency, targetDate)] = rate;
        }
        finally
        {
            _semaphore.Release();
        }
        
        return _rates;
    }


    public Dictionary<(Currency, Currency, DateOnly), double> Get() 
        => _rates;


    private void UploadFromFile()
    {
        if (!File.Exists(_rateSourcePath))
        using (File.Create(_rateSourcePath)) { }

        foreach (var line in File.ReadLines(_rateSourcePath))
        {
            var parts = line.Split(',');
            if (parts.Length != 4)
                continue;

            var baseCurrency = Enum.Parse<Currency>(parts[0]);
            var targetCurrency = Enum.Parse<Currency>(parts[1]);
            var date = DateOnly.Parse(parts[2]);
            var rate = double.Parse(parts[3]);

            _rates[(baseCurrency, targetCurrency, date)] = rate;
        }
    }


    private readonly Dictionary<(Currency, Currency, DateOnly), double> _rates = [];
    private readonly string _rateSourcePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
}
