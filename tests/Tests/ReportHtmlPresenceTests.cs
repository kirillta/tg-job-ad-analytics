using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TgJobAdAnalytics.Models.Reports.Metadata;
using TgJobAdAnalytics.Services.Reports.Metadata;
using TgJobAdAnalytics.Services.Localization;
using TgJobAdAnalytics.Services.Reports.Html.Testing;

namespace Tests;

public class ReportHtmlPresenceTests
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
            },
            ["ru"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["report.title.suffix"] = "????????? ???????",
                ["report.description.default"] = "???????? ?? ?????????",
                ["report.description.with_stats"] = "??????? {median} P90 {p90} ?????????? {count}"
            }
        };

        public string Get(string locale, string key) => _data[locale][key];
    }

    [Fact]
    public void RenderedHtml_ShouldContainCoreMetaTags()
    {
        var options = Options.Create(new SiteMetadataOptions
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Analytics",
            DefaultOgImagePath = "/assets/og/default.png",
            Locales = new List<string> { "en", "ru" },
            PrimaryLocale = "en",
            JsonLdType = JsonLdType.Article
        });

        var builder = new MetadataBuilder(options, NullLogger<MetadataBuilder>.Instance, new TestLocalizationProvider());
        var metadata = builder.Build(locale: "en", kpis: null, persistedPublishedUtc: null, generatedUtc: DateTime.UtcNow);

        var html = ReportTestRenderer.Render(metadata, templatesRoot: Path.Combine("src", "TgJobAdAnalytics", "Views", "Reports"));

        Assert.Contains("<meta name=\"description\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<meta property=\"og:title\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<meta property=\"og:description\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<meta name=\"twitter:card\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/ld+json", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hreflang=\"en\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hreflang=\"ru\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hreflang=\"x-default\"", html, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void JsonLd_ShouldContainHeadlineAndDates()
    {
        var options = Options.Create(new SiteMetadataOptions
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Analytics",
            DefaultOgImagePath = "/img.png",
            Locales = new List<string> { "en" },
            PrimaryLocale = "en",
            JsonLdType = JsonLdType.Article
        });

        var builder = new MetadataBuilder(options, NullLogger<MetadataBuilder>.Instance, new TestLocalizationProvider());
        var metadata = builder.Build(locale: "en", kpis: null, persistedPublishedUtc: null, generatedUtc: DateTime.UtcNow);

        var html = ReportTestRenderer.Render(metadata, templatesRoot: Path.Combine("src", "TgJobAdAnalytics", "Views", "Reports"));

        var jsonLdMatch = Regex.Match(html, "<script type=\"application/ld\\+json\">(?<json>.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Assert.True(jsonLdMatch.Success, "JSON-LD script block not found");

        var json = jsonLdMatch.Groups["json"].Value;
        Assert.Contains("\"headline\"", json);
        Assert.Contains("\"datePublished\"", json);
        Assert.Contains("\"dateModified\"", json);
    }
}
