using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class RateSourceManager
{
    public RateSourceManager(ILogger<RateSourceManager> logger, IOptions<RateOptions> rateOptions)
    {
        _logger = logger;
        _rateOptions = rateOptions.Value;

        UploadFromFile();
    }


    public async Task Add(List<Rate> rates)
    {
        _logger.LogInformation("Adding {Count} rates to the source file: {RateSourcePath}", rates.Count, _rateOptions.RateSourcePath);

        using var writer = new StreamWriter(_rateOptions.RateSourcePath, true);

        var ratesByDate = rates.GroupBy(x => x.TargetDate)
            .ToDictionary(g => g.Key, g => g.ToList());

        var targetDate = ratesByDate.Keys.Min();
        var lastDate = ratesByDate.Keys.Max();

        while (targetDate <= lastDate)
        {
            if (ratesByDate.TryGetValue(targetDate, out var currentRates))
            {
                await WriteRates(writer, targetDate, currentRates!);
            }
            else
            {
                var previousDate = targetDate.AddDays(-1);
                ratesByDate.TryGetValue(previousDate, out var previousRates);
                while (previousRates is null && previousDate >= ratesByDate.Keys.Min())
                {
                    previousDate = previousDate.AddDays(-1);
                    ratesByDate.TryGetValue(previousDate, out previousRates);
                }

                await WriteRates(writer, targetDate, previousRates!);
            }

            targetDate = targetDate.AddDays(1);
        }


        async Task WriteRates(StreamWriter writer, DateOnly targetDate, List<Rate> rates)
        {
            foreach (var rate in rates)
            {
                if (_rates.ContainsKey((rate.TargetCurrency, targetDate)))
                    continue;

                await writer.WriteLineAsync($"{rate.BaseCurrency},{rate.TargetCurrency},{targetDate},{rate.Value}");
                _rates[(rate.TargetCurrency, targetDate)] = rate;
            }
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
        if (!File.Exists(_rateOptions.RateSourcePath))
        {
            _logger.LogWarning("Rate source file not found: {RateSourcePath}. Creating a new file.", _rateOptions.RateSourcePath);
            File.WriteAllText(_rateOptions.RateSourcePath, string.Empty);
        }

        foreach (var line in File.ReadLines(_rateOptions.RateSourcePath))
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


    private readonly ILogger<RateSourceManager> _logger;
    private readonly Dictionary<(Currency Target, DateOnly Date), Rate> _rates = [];
    private readonly RateOptions _rateOptions;
}
