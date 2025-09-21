namespace TgJobAdAnalytics.Models.Reports.Html;

internal readonly record struct ReportPageMetadata(
    string Title,
    string Description,
    string Locale,
    string CanonicalUrl,
    string OgImageUrl,
    string KeywordsCsv,
    string PublishedIso,
    string ModifiedIso,
    List<(string locale, string url)> HreflangAlternates,
    string JsonLd
)
{
    public string Title { get; } = Title;
    public string Description { get; } = Description;
    public string Locale { get; } = Locale;
    public string CanonicalUrl { get; } = CanonicalUrl;
    public string OgImageUrl { get; } = OgImageUrl;
    public string KeywordsCsv { get; } = KeywordsCsv;
    public string PublishedIso { get; } = PublishedIso;
    public string ModifiedIso { get; } = ModifiedIso;
    public List<(string locale, string url)> HreflangAlternates { get; } = HreflangAlternates;
    public string JsonLd { get; } = JsonLd;
}
