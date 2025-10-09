using Microsoft.Extensions.Options;
using System.Globalization;
using System.Xml.Linq;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// HTTP client for retrieving historical currency exchange rates from the configured remote rate API.
/// Produces forward (base→target) and inverse (target→base) <see cref="Rate"/> entries for each returned day.
/// </summary>
public sealed class RateApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateApiClient"/>.
    /// </summary>
    /// <param name="rateOptions">Options providing the base API URL.</param>
    public RateApiClient(IOptions<RateOptions> rateOptions)
    {
        _client = new HttpClient
        {
            BaseAddress = rateOptions.Value.RateApiUrl
        };
    }


    /// <summary>
    /// Retrieves historical exchange rates between the specified base and target currencies starting at the given initial date up to today (UTC).
    /// Returns both forward and inverse rates for convenience.
    /// </summary>
    /// <param name="baseCurrency">Currency considered the base for conversion.</param>
    /// <param name="targetCurrency">Currency to convert into (API specific code is resolved internally).</param>
    /// <param name="initialtDate">Start date (inclusive) for historical rate retrieval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of <see cref="Rate"/> entries (forward and inverse) ordered as received from the API.</returns>
    public async Task<List<Rate>> Get(Currency baseCurrency, Currency targetCurrency, DateOnly initialtDate, CancellationToken cancellationToken)
    {
        var currencyCode = GetCurrencyCode(targetCurrency);
        var address = $"?date_req1={initialtDate:dd/MM/yyyy}&date_req2={DateOnly.FromDateTime(DateTime.UtcNow):dd/MM/yyyy}&VAL_NM_RQ={currencyCode}";
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        
        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return ParseRates(responseStream, baseCurrency, targetCurrency);
    }


    private static string GetCurrencyCode(Currency targetCurrency)
    {
        return targetCurrency switch
        {
            Currency.USD => "R01235",
            Currency.EUR => "R01239",
            _ => throw new ArgumentException("Unknown currency")
        };
    }


    private static List<Rate> ParseRates(Stream xmlStream, Currency baseCurrency, Currency targetCurrency)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var rates = new List<Rate>();
        var document = XDocument.Load(xmlStream);

        foreach (var record in document.Descendants("Record"))
        {
            var date = DateOnly.Parse(record.Attribute("Date")!.Value);
            var value = double.Parse(record.Element("Value")!.Value.Replace(',', '.'), CultureInfo.InvariantCulture);

            rates.Add(new Rate(baseCurrency, targetCurrency, date, value));
            rates.Add(new Rate(targetCurrency, baseCurrency, date, Math.Round(1 / value, 4)));
        }

        return rates;
    }


    private readonly HttpClient _client;
}
