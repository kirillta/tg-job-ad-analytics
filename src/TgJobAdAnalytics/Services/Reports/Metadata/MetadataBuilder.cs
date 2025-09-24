using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Reports.Metadata;
using TgJobAdAnalytics.Services.Localization;

namespace TgJobAdAnalytics.Services.Reports.Metadata;

/// <summary>
/// Builds SEO / Open Graph / Twitter / JSON-LD metadata models for the evergreen report page.
/// </summary>
public sealed partial class MetadataBuilder
{
    public MetadataBuilder(IOptions<SiteMetadataOptions> options, ILogger<MetadataBuilder> logger, ILocalizationProvider localizationProvider)
    {
        _options = options.Value;
        _logger = logger;
        _localization = localizationProvider;
    }


    /// <summary>
    /// Build a <see cref="MetadataModel"/> for the provided locale.
    /// </summary>
    /// <param name="locale">Target locale (must exist in configured options).</param>
    /// <param name="kpis">Optional KPI dictionary (e.g., median, p90 etc.) used to enrich description & keywords.</param>
    /// <param name="persistedPublishedUtc">Previously persisted published timestamp or null if first generation.</param>
    /// <param name="generatedUtc">Current generation time (used as modified & possibly published).</param>
    /// <param name="extraKeywords">Optional additional keyword set.</param>
    public MetadataModel Build(string locale, IReadOnlyDictionary<string, double>? kpis, DateTime? persistedPublishedUtc, DateTime generatedUtc, IEnumerable<string>? extraKeywords = null)
    {
        ValidateOptions();

        locale = NormalizeLocale(locale);
        EnsureLocaleSupported(locale);

        var publishedUtc = persistedPublishedUtc ?? generatedUtc;
        var modifiedUtc = generatedUtc;

        var canonicalUrl = BuildCanonicalUrl(locale);
        var ogImageUrl = BuildOgImageUrl();

        var (title, description) = BuildText(locale, kpis);

        var keywords = BuildKeywords(kpis, extraKeywords);

        var hreflangAlternates = BuildHreflangAlternates();

        return new MetadataModel(
            Title: title,
            Description: description,
            CanonicalUrl: canonicalUrl,
            Locale: locale,
            PublishedUtc: publishedUtc,
            ModifiedUtc: modifiedUtc,
            OgImageUrl: ogImageUrl,
            Keywords: keywords,
            JsonLdType: _options.JsonLdType,
            HreflangAlternates: hreflangAlternates
        );
    }


    private (string Title, string Description) BuildText(string locale, IReadOnlyDictionary<string, double>? kpis)
    {
        var suffix = _localization.Get(locale, "report.title.suffix");
        var title = _options.SiteName + " — " + suffix;

        string? median = TryGetRounded(kpis, "median");
        string? p90 = TryGetRounded(kpis, "p90");
        string? count = TryGetRounded(kpis, "count");

        string descriptionTemplate;
        if (median is not null && p90 is not null && count is not null)
        {
            descriptionTemplate = _localization.Get(locale, "report.description.with_stats")
                .Replace("{median}", median)
                .Replace("{p90}", p90)
                .Replace("{count}", count);
        }
        else
        {
            descriptionTemplate = _localization.Get(locale, "report.description.default");
        }

        var description = TrimToLength(descriptionTemplate, 160);
        return (title, description);
    }


    private static string? TryGetRounded(IReadOnlyDictionary<string, double>? kpis, string key)
    {
        if (kpis is null)
            return null;

        if (!kpis.TryGetValue(key, out var value))
            return null;

        if (double.IsNaN(value) || double.IsInfinity(value))
            return null;

        return value % 1 == 0 
            ? value.ToString("N0") 
            : value.ToString("N2");
    }


    private static List<string> BuildKeywords(IReadOnlyDictionary<string, double>? kpis, IEnumerable<string>? extra)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "salary",
            "telegram",
            "job ads",
            "analytics"
        };

        if (kpis is not null && kpis.Count > 0)
            set.Add("statistics");

        if (extra is not null)
        {
            foreach (var kw in extra)
            {
                var sanitized = SanitizeKeyword(kw);
                if (!string.IsNullOrWhiteSpace(sanitized))
                    set.Add(sanitized);
            }
        }

        return [.. set.Take(10).Select(x => x.ToLowerInvariant())];
    }


    private static string SanitizeKeyword(string keyword)
        => SanitizedKeywords().Replace(keyword.ToLowerInvariant().Trim(), string.Empty);


    private List<HreflangAlternate> BuildHreflangAlternates()
    {
        var list = new List<HreflangAlternate>(_options.Locales.Count + 1);
        foreach (var loc in _options.Locales)
            list.Add(new HreflangAlternate(NormalizeLocale(loc), BuildCanonicalUrl(loc)));

        list.Add(new HreflangAlternate("x-default", BuildCanonicalUrl(_options.PrimaryLocale)));

        return list;
    }


    private string BuildCanonicalUrl(string locale)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/{locale}/reports/index.html";
    }


    private string BuildOgImageUrl()
    {
        if (string.IsNullOrWhiteSpace(_options.DefaultOgImagePath))
            return string.Empty;

        if (_options.DefaultOgImagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || _options.DefaultOgImagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return _options.DefaultOgImagePath;

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var path = _options.DefaultOgImagePath.StartsWith('/') ? _options.DefaultOgImagePath : "/" + _options.DefaultOgImagePath;

        return baseUrl + path;
    }


    private static string TrimToLength(string value, int max)
    {
        if (value.Length <= max)
            return value;

        var truncated = value[..max];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 40)
            truncated = truncated[..lastSpace];

        return truncated.TrimEnd('.', ',', ';', ':') + "...";
    }


    private static string NormalizeLocale(string locale)
        => locale.Trim().Replace('_', '-');


    private void EnsureLocaleSupported(string locale)
    {
        if (!_options.Locales.Any(l => string.Equals(NormalizeLocale(l), locale, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogError("Locale '{Locale}' is not configured. Supported: {Supported}", locale, string.Join(", ", _options.Locales));
            throw new InvalidOperationException($"Locale '{locale}' is not configured. Supported: {string.Join(", ", _options.Locales)}");
        }
    }


    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogError("SiteMetadataOptions.BaseUrl is not configured.");
            throw new InvalidOperationException("SiteMetadataOptions.BaseUrl must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.SiteName))
        {
            _logger.LogError("SiteMetadataOptions.SiteName is not configured.");
            throw new InvalidOperationException("SiteMetadataOptions.SiteName must be configured.");
        }

        if (_options.Locales is null || _options.Locales.Count == 0)
        {
            _logger.LogError("SiteMetadataOptions.Locales is not configured.");
            throw new InvalidOperationException("At least one locale must be configured in SiteMetadataOptions.Locales.");
        }

        if (string.IsNullOrWhiteSpace(_options.PrimaryLocale))
        {   
            _logger.LogError("SiteMetadataOptions.PrimaryLocale is not configured.");
            throw new InvalidOperationException("SiteMetadataOptions.PrimaryLocale must be configured.");
        }
    }


    [GeneratedRegex(@"[^a-z0-9\- ]")]
    private static partial Regex SanitizedKeywords();


    private readonly SiteMetadataOptions _options;
    private readonly ILogger<MetadataBuilder> _logger;
    private readonly ILocalizationProvider _localization;
}
