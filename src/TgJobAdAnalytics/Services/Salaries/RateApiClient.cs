using System.Globalization;
using System.Xml.Linq;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

internal class RateApiClient
{
    public RateApiClient()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://www.cbr.ru/scripts/XML_dynamic.asp")
        };
    }


    public async Task<List<Rate>> Get(Currency baseCurrency, Currency targetCurrency, DateOnly initialtDate)
    {
        var currencyCode = GetCurrencyCode(targetCurrency);
        var address = $"?date_req1={initialtDate:dd/MM/yyyy}&date_req2={DateOnly.FromDateTime(DateTime.Now):dd/MM/yyyy}&VAL_NM_RQ={currencyCode}";
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync();

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
