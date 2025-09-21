using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using TgJobAdAnalytics.Models.Reports.Html;
using TgJobAdAnalytics.Models.Reports.Metadata;

namespace TgJobAdAnalytics.Services.Reports.Html.Testing;

/// <summary>
/// Helper renderer exposed for tests to render the main report template with supplied metadata.
/// </summary>
public static class ReportTestRenderer
{
    public static string Render(MetadataModel metadata, string templatesRoot)
    {
        var groups = new List<ReportItemGroup>();
        var dataSources = new List<DataSourceModel>();
        var reportModel = new ReportModel(
            reportGroups: groups,
            reportDate: DateOnly.FromDateTime(DateTime.UtcNow),
            dataSources: dataSources,
            metadata: new ReportPageMetadata(
                Title: metadata.Title,
                Description: metadata.Description,
                Locale: metadata.Locale,
                CanonicalUrl: metadata.CanonicalUrl,
                OgImageUrl: metadata.OgImageUrl,
                KeywordsCsv: metadata.KeywordsCsv,
                PublishedIso: metadata.PublishedUtc.ToString("O"),
                ModifiedIso: metadata.ModifiedUtc.ToString("O"),
                HreflangAlternates: metadata.HreflangAlternates.Select(h => (h.Locale, h.Url)).ToList(),
                JsonLd: BuildJsonLd(metadata)
            )
        );

        var mainTemplatePath = Path.Combine(templatesRoot, "MainTemplate.sbn");
        var templateContent = File.ReadAllText(mainTemplatePath);
        var template = Template.Parse(templateContent);

        var scriptObject = new ScriptObject();
        scriptObject.Import(reportModel);
        var context = new TemplateContext { EnableRelaxedMemberAccess = true };
        context.PushGlobal(scriptObject);
        return template.Render(context);
    }


    private static string BuildJsonLd(MetadataModel m)
    {
        var article = new
        {
            @context = "https://schema.org",
            @type = "Article",
            mainEntityOfPage = new { @type = "WebPage", @id = m.CanonicalUrl },
            headline = m.Title,
            description = m.Description,
            datePublished = m.PublishedUtc.ToString("O"),
            dateModified = m.ModifiedUtc.ToString("O"),
            inLanguage = m.Locale,
            author = new { @type = "Organization", name = "" },
            publisher = new { @type = "Organization", name = "" },
            image = string.IsNullOrEmpty(m.OgImageUrl) ? null : m.OgImageUrl,
            keywords = string.Join(", ", m.Keywords)
        };

        return JsonSerializer.Serialize(article);
    }
}
