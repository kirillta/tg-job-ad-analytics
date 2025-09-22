using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Reports.Metadata;
using TgJobAdAnalytics.Services.Reports.Metadata;
using TgJobAdAnalytics.Services.Localization;

namespace Tests;

public class MetadataBuilderTests
{
    private sealed class TestLocalizationProvider : ILocalizationProvider
    {
        private readonly Dictionary<string, Dictionary<string, string>> _data = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["report.title.suffix"] = "Salary & Job Ad Analytics",
                ["report.description.default"] = "Default description",
                ["report.description.with_stats"] = "Median {median} P90 {p90} Count {count}"
            }
        };

        public string Get(string locale, string key) => _data[locale][key];
    }

    [Fact]
    public void Build_ShouldPopulateCoreFields()
    {
        var options = Options.Create(new SiteMetadataOptions
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Analytics",
            DefaultOgImagePath = "/assets/og/default.png",
            Locales = new List<string> { "en" },
            PrimaryLocale = "en",
            JsonLdType = JsonLdType.Article
        });

        var builder = new MetadataBuilder(options, NullLogger<MetadataBuilder>.Instance, new TestLocalizationProvider());

        var metadata = builder.Build(locale: "en", kpis: null, persistedPublishedUtc: null, generatedUtc: DateTime.UtcNow);

        Assert.Equal("en", metadata.Locale);
        Assert.StartsWith("https://example.com/en/reports/index.html", metadata.CanonicalUrl);
        Assert.NotEmpty(metadata.Title);
        Assert.NotEmpty(metadata.Description);
        Assert.Contains("salary", metadata.Keywords);
    }


    [Fact]
    public void Build_ShouldReusePublishedTimestamp()
    {
        var options = Options.Create(new SiteMetadataOptions
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Analytics",
            DefaultOgImagePath = "/assets/og/default.png",
            Locales = new List<string> { "en" },
            PrimaryLocale = "en",
            JsonLdType = JsonLdType.Article
        });

        var builder = new MetadataBuilder(options, NullLogger<MetadataBuilder>.Instance, new TestLocalizationProvider());
        var published = DateTime.UtcNow.AddDays(-5);

        var metadata = builder.Build(locale: "en", kpis: null, persistedPublishedUtc: published, generatedUtc: DateTime.UtcNow);

        Assert.Equal(published, metadata.PublishedUtc);
        Assert.NotEqual(published, metadata.ModifiedUtc);
    }
}
