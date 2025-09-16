using System.Text.RegularExpressions;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Salaries;

namespace Tests.Helpers;

internal sealed partial class SalaryProcessingResultHelper
{
    public SalaryProcessingResultHelper(SalaryProcessingService processingService)
    {
        _processingService = processingService;
    }


    // Simple parser used by tests to convert a short text into a SalaryEntity and process it
    public SalaryProcessingResult Get(string input, DateOnly date)
    {
        var (lower, upper, currency) = Parse(input);
        var entity = new SalaryEntity
        {
            Date = date,
            Currency = currency,
            LowerBound = lower,
            UpperBound = upper,
            Period = Period.Month,
            Status = ProcessingStatus.Extracted
        };

        var processed = _processingService.Process(entity);
        if (processed is null)
        {
            return new SalaryProcessingResult
            {
                LowerBound = double.NaN,
                UpperBound = double.NaN,
                Currency = Currency.Unknown,
                CurrencyNormalized = Currency.Unknown,
                LowerBoundNormalized = double.NaN,
                UpperBoundNormalized = double.NaN
            };
        }

        return new SalaryProcessingResult
        {
            LowerBound = processed.LowerBound ?? double.NaN,
            UpperBound = processed.UpperBound ?? double.NaN,
            Currency = processed.Currency ?? Currency.Unknown,
            CurrencyNormalized = processed.CurrencyNormalized,
            LowerBoundNormalized = processed.LowerBoundNormalized,
            UpperBoundNormalized = processed.UpperBoundNormalized
        };


        static (double lower, double upper, Currency currency) Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (double.NaN, double.NaN, Currency.Unknown);

            var normalized = TextNormalizer.NormalizeTextEntry(text);

            var currency = Currency.Unknown;
            if (normalized.Contains('$')) 
                currency = Currency.USD;
            else if (normalized.Contains('€')) 
                currency = Currency.EUR;
            else if (normalized.Contains('₽') || normalized.Contains(" руб")) 
                currency = Currency.RUB;

            // extract numbers like 50k, 100000, 60-80k
            var numbers = new List<double>();
            foreach (Match m in SalaryAmountRegex().Matches(normalized))
            {
                var token = m.Value.ToLowerInvariant();
                double val = double.TryParse(token.TrimEnd('k'), out var v) ? v : double.NaN;
                if (token.EndsWith('k')) val *= 1000;
                numbers.Add(val);
            }

            if (numbers.Count == 0)
                return (double.NaN, double.NaN, currency);

            if (numbers.Count == 1)
            {
                // If text contains "до" treat as upper only; otherwise assume a range with same bounds
                var isUpper = normalized.Contains(" до ", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("до ");
                return isUpper ? (double.NaN, numbers[0], currency) : (numbers[0], numbers[0], currency);
            }

            var lower = Math.Min(numbers[0], numbers[1]);
            var upper = Math.Max(numbers[0], numbers[1]);

            return (lower, upper, currency);
        }
    }


    private readonly SalaryProcessingService _processingService;

    [GeneratedRegex("(?:(?:\\d+)(?:k)?)", RegexOptions.Compiled)]
    private static partial Regex SalaryAmountRegex();
}
