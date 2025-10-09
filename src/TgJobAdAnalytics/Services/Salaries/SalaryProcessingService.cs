using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Applies validation, boundary normalization, period standardization (daily → monthly) and currency normalization
/// to extracted salary entities. Produces enriched entities or returns null on irrecoverable failure.
/// </summary>
public sealed class SalaryProcessingService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryProcessingService"/>.
    /// </summary>
    /// <param name="baseCurrency">Canonical base currency used for normalization.</param>
    /// <param name="rateService">Exchange rate service for currency conversion.</param>
    public SalaryProcessingService(Currency baseCurrency, IRateService rateService)
    {
        _baseCyrrency = baseCurrency;
        _rateService = rateService;
    }


    /// <summary>
    /// Processes the supplied salary entity through validation, boundary consistency correction, period conversion
    /// (day → month) and currency normalization producing an enriched entity, or null on failure.
    /// </summary>
    /// <param name="salaryResponse">Raw salary entity to process.</param>
    /// <returns>Enriched entity or null if processing failed.</returns>
    public SalaryEntity? Process(SalaryEntity salaryResponse)
    {
        salaryResponse = Validate(salaryResponse);
        salaryResponse = EnsureBoundariesConsistency(salaryResponse);
        salaryResponse = ConvertPeriodToMonthly(salaryResponse);

        return Enrich(salaryResponse);
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
