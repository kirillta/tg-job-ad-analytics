using NSubstitute;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Services.Salaries;

namespace Tests;

public class SalaryServiceTests
{
    public SalaryServiceTests()
    {
        _rateService = Substitute.For<IRateService>();
        _rateService.Get(Arg.Any<Currency>(), Arg.Any<Currency>(), Arg.Any<DateOnly>()).Returns(1.1);

        _service = new SalaryProcessingService(Currency.USD, _rateService);
    }


    [Theory]
    [InlineData("50k-70k USD", 50000, 70000, Currency.USD)]
    [InlineData("от 150к до 200к руб", 150000, 200000, Currency.RUB)]
    [InlineData("$60-80k", 60000, 80000, Currency.USD)]
    [InlineData("до 100к", double.NaN, 100000, Currency.RUB)]
    public void Get_ValidSalaryRanges_ParsesCorrectly(string input, double expectedLower, double expectedUpper, Currency expectedCurrency)
    {
        var date = new DateOnly(2024, 1, 1);

        var result = _service.Get(input, date);

        Assert.Equal(expectedLower, result.LowerBound);
        Assert.Equal(expectedUpper, result.UpperBound);
        Assert.Equal(expectedCurrency, result.Currency);
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
        var service = new SalaryProcessingService(baseCurrency, _rateService);

        var result = service.Get(input, date);

        Assert.Equal(expectedLowerNormalized, result.LowerBoundNormalized);
        Assert.Equal(expectedUpperNormalized, result.UpperBoundNormalized);
    }


    private readonly SalaryProcessingService _service;
    private readonly IRateService _rateService;
}