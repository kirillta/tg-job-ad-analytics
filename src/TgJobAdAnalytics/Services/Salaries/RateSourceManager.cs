using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

internal class RateSourceManager
{
    public RateSourceManager(string rateSourcePath)
    {
        _rateSourcePath = rateSourcePath;

        UploadFromFile();
    }


    public async Task Add(List<Rate> rates)
    {
        using var writer = new StreamWriter(_rateSourcePath, true);
        foreach (var rate in rates)
        {
            if (_rates.ContainsKey((rate.TargetCurrency, rate.TargetDate)))
                continue;

            await writer.WriteLineAsync($"{rate.BaseCurrency},{rate.TargetCurrency},{rate.TargetDate},{rate.Value}");
            _rates[(rate.TargetCurrency, rate.TargetDate)] = rate;
        }
    }


    public Dictionary<(Currency Target, DateOnly Date), Rate> Get() 
        => _rates;


    public DateOnly GetMaximalDate()
    {
        if (_rates.Count == 0)
            return DateOnly.FromDateTime(DateTime.Now);

        return _rates.Keys.Select(x => x.Date).Max();
    }


    public DateOnly GetMinimalDate()
    {
        if (_rates.Count == 0)
            return DateOnly.FromDateTime(DateTime.Now);

        return _rates.Keys.Select(x => x.Date).Min();
    }


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

            _rates[(targetCurrency, date)] = new Rate(baseCurrency, targetCurrency, date, rate);
        }
    }


    private readonly Dictionary<(Currency Target, DateOnly Date), Rate> _rates = [];
    private readonly string _rateSourcePath;
}
