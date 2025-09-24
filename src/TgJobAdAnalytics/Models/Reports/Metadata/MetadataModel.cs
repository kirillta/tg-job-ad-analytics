using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Models.Reports.Metadata;

public sealed record MetadataModel(
    string Title,
    string Description,
    string CanonicalUrl,
    string Locale,
    DateTime PublishedUtc,
    DateTime ModifiedUtc,
    string OgImageUrl,
    IReadOnlyList<string> Keywords,
    JsonLdType JsonLdType,
    IReadOnlyList<HreflangAlternate> HreflangAlternates
)
{
    [JsonIgnore]
    public string KeywordsCsv => string.Join(", ", Keywords);
}
