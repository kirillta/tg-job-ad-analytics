using NSubstitute;
using Tests.Helpers;
using TgJobAdAnalytics.Models.Salaries.Enums;
using TgJobAdAnalytics.Services.Salaries;

namespace Tests;

public class SalaryServiceTests
{
    public SalaryServiceTests()
    {
        _rateService = Substitute.For<IRateService>();
        _rateService.Get(Arg.Any<Currency>(), Arg.Any<Currency>(), Arg.Any<DateOnly>()).Returns(1.1);

        _service = new SalaryProcessingResultHelper(new SalaryProcessingService(Currency.USD, _rateService));
    }


    [Theory]
    [InlineData("invalid text")]
    [InlineData("")]
    public void Get_InvalidInput_ReturnsUnknownCurrency(string input)
    {
        var date = new DateOnly(2024, 1, 1);

        var result = _service.Get(input, date);

        Assert.Equal(Currency.Unknown, result.Currency);
        Assert.True(double.IsNaN(result.LowerBound));
        Assert.True(double.IsNaN(result.UpperBound));
    }


    [Theory]
    [InlineData("100k-120k $", Currency.USD, 100000, 120000)]
    [InlineData("100k-120k €", Currency.USD, 110000, 132000)] // Assuming 1.1 rate
    public void Get_CurrencyNormalization_ConvertsCorrectly(string input, Currency baseCurrency,
        double expectedLowerNormalized, double expectedUpperNormalized)
    {
        var date = new DateOnly(2024, 1, 1);
        var service = new SalaryProcessingResultHelper(new SalaryProcessingService(baseCurrency, _rateService));

        var result = service.Get(input, date);

        Assert.Equal(expectedLowerNormalized, result.LowerBoundNormalized);
        Assert.Equal(expectedUpperNormalized, result.UpperBoundNormalized);
    }


    private readonly SalaryProcessingResultHelper _service;
    private readonly IRateService _rateService;
}