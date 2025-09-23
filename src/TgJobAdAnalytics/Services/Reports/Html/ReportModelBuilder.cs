using System.Text.Json;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Reports.Metadata;

namespace TgJobAdAnalytics.Services.Reports.Html;

internal class ReportModelBuilder
{
    public static ReportModel Build(List<ReportItemGroup> groups, List<DataSourceModel> dataSources, MetadataModel metadata, Dictionary<string, object>? localization = null)
        => new(
            reportGroups: groups,
            reportDate: DateOnly.FromDateTime(DateTime.UtcNow),
            dataSources: dataSources,
            metadata: BuildMetadata(metadata),
            localization: localization ?? new Dictionary<string, object>(),
            locales: metadata.HreflangAlternates.Select(a => a.Locale).Prepend(metadata.Locale).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            currentLocale: metadata.Locale
        );


    private static ReportPageMetadata BuildMetadata(MetadataModel metadataModel)
        => new(
            Title: metadataModel.Title,
            Description: metadataModel.Description,
            Locale: metadataModel.Locale,
            CanonicalUrl: metadataModel.CanonicalUrl,
            OgImageUrl: metadataModel.OgImageUrl,
            KeywordsCsv: metadataModel.KeywordsCsv,
            PublishedIso: metadataModel.PublishedUtc.ToString("O"),
            ModifiedIso: metadataModel.ModifiedUtc.ToString("O"),
            HreflangAlternates: metadataModel.HreflangAlternates.Select(h => (h.Locale, h.Url)).ToList(),
            JsonLd: BuildJsonLd(metadataModel)
        );


    private static string BuildJsonLd(MetadataModel metadataModel)
    {
        if (metadataModel.JsonLdType == JsonLdType.Dataset)
        {
            var dataset = new
            {
                @context = "https://schema.org",
                @type = "Dataset",
                name = metadataModel.Title,
                description = metadataModel.Description,
                @id = metadataModel.CanonicalUrl,
                url = metadataModel.CanonicalUrl,
                inLanguage = metadataModel.Locale,
                dateModified = metadataModel.ModifiedUtc.ToString("O"),
                keywords = metadataModel.Keywords
            };

            return JsonSerializer.Serialize(dataset);
        }

        var article = new
        {
            @context = "https://schema.org",
            @type = "Article",
            mainEntityOfPage = new { @type = "WebPage", @id = metadataModel.CanonicalUrl },
            headline = metadataModel.Title,
            description = metadataModel.Description,
            datePublished = metadataModel.PublishedUtc.ToString("O"),
            dateModified = metadataModel.ModifiedUtc.ToString("O"),
            inLanguage = metadataModel.Locale,
            author = new { @type = "Organization", name = "" },
            publisher = new { @type = "Organization", name = "" },
            image = string.IsNullOrEmpty(metadataModel.OgImageUrl) ? null : metadataModel.OgImageUrl,
            keywords = string.Join(", ", metadataModel.Keywords)
        };

        return JsonSerializer.Serialize(article);
    }
}
