using System.Text.RegularExpressions;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Services.Messages;

namespace TgJobAdAnalytics.Services.Salaries;

public class SalaryProcessingService
{
    public SalaryProcessingService(Currency baseCurrency, IRateService rateService)
    {
        _baseCyrrency = baseCurrency;
        _rateService = rateService;
    }


    public SalaryEntity? Process(SalaryEntity salaryResponse)
    {
        salaryResponse = Validate(salaryResponse);
        salaryResponse = EnsureBoundariesConsistency(salaryResponse);
        salaryResponse = ConvertPeriodToMonthly(salaryResponse);

        return Enrich(salaryResponse);
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

        var processed = Process(entity);
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
            foreach (Match m in Regex.Matches(normalized, "(?:(?:\\d+)(?:k)?)"))
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


    private static SalaryEntity ConvertPeriodToMonthly(SalaryEntity salary)
    {
        if (salary.Status is ProcessingStatus.Failed)
            return salary;

        if (salary.Period is null || salary.Period is Period.Unknown || salary.Period is Period.Project)
            salary.Status = ProcessingStatus.Failed;

        if (salary.Period is Period.Day)
        {
            var lowerMonthly = double.NaN;
            var upperMonthly = double.NaN;

            if (salary.LowerBound is not double.NaN)
                lowerMonthly = Round(salary.LowerBound!.Value);

            if (salary.UpperBound is not double.NaN)
                upperMonthly = Round(salary.UpperBound!.Value);

            salary.LowerBound = lowerMonthly;
            salary.UpperBound = upperMonthly;
            salary.Period = Period.Month;
        }

        return salary;
    }


    private SalaryEntity? Enrich(SalaryEntity salary)
    { 
        if (salary.Status is ProcessingStatus.Failed)
            return null;
        
        salary.Status = ProcessingStatus.Enriched;

        if (salary.Currency == _baseCyrrency)
        { 
            salary.CurrencyNormalized = salary.Currency.Value;
            salary.LowerBoundNormalized = salary.LowerBound!.Value;
            salary.UpperBoundNormalized = salary.UpperBound!.Value;

            return salary;
        }

        salary.CurrencyNormalized = _baseCyrrency;
        salary.LowerBoundNormalized = NormalizeInternal(salary.LowerBound!.Value);
        salary.UpperBoundNormalized = NormalizeInternal(salary.UpperBound!.Value);

        return salary;


        double NormalizeInternal(double value)
        {
            if (double.IsNaN(value) || value == 0)
                return value;

            var rate = _rateService.Get(_baseCyrrency, salary.Currency!.Value, salary.Date);
            var amount = value * rate;

            return Round(amount);
        }
    }


    private static SalaryEntity EnsureBoundariesConsistency(SalaryEntity salary)
    {
        if (salary.Status is ProcessingStatus.Failed)
            return salary;

        switch (salary.LowerBound)
        {
            case not double.NaN when salary.UpperBound is not double.NaN:
            {
                if (salary.LowerBound > salary.UpperBound)
                    (salary.UpperBound, salary.LowerBound) = (salary.LowerBound, salary.UpperBound);

                break;
            }
            case not double.NaN when salary.UpperBound is double.NaN:
                salary.UpperBound = salary.LowerBound;
                break;
            case double.NaN when salary.UpperBound is not double.NaN:
                salary.LowerBound = salary.UpperBound;
                break;
        }

        return salary;
    }


    private static double Round(double value) 
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);


    private static SalaryEntity Validate(SalaryEntity salary)
    {
        if (salary.Status is ProcessingStatus.Failed)
            return salary;

        if (salary.Currency is null || salary.Currency is Currency.Unknown)
            salary.Status = ProcessingStatus.Failed;

        salary.LowerBound ??= double.NaN;
        salary.UpperBound ??= double.NaN;

        if (salary.LowerBound is double.NaN && salary.UpperBound is double.NaN)
            salary.Status = ProcessingStatus.Failed;

        return salary;
    }


    private readonly Currency _baseCyrrency;
    private readonly IRateService _rateService;
}
