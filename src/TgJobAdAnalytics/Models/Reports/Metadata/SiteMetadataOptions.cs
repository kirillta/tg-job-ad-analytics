using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Models.Reports.Metadata;

/// <summary>
/// Global site metadata options used for generating SEO, Open Graph, Twitter Card and JSON-LD data.
/// </summary>
public sealed class SiteMetadataOptions
{
    /// <summary>
    /// Absolute base URL of the site (e.g. https://example.com). No trailing slash.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Public facing site name.
    /// </summary>
    [Required]
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// Default Open Graph / social share image path (absolute or application relative starting with '/').
    /// </summary>
    [Required]
    public string DefaultOgImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Locales supported by the site (ISO 639-1 optionally with region, e.g. en, en-US, ru).
    /// </summary>
    [Required]
    public List<string> Locales { get; set; } = new();

    /// <summary>
    /// Primary locale used for x-default hreflang.
    /// </summary>
    [Required]
    public string PrimaryLocale { get; set; } = string.Empty;

    /// <summary>
    /// JSON-LD entity type to emit (Article or Dataset). Defaults to Article.
    /// </summary>
    public JsonLdType JsonLdType { get; set; } = JsonLdType.Article;

    /// <summary>
    /// Path where localization JSON files are stored (relative to base directory if not rooted).
    /// </summary>
    public string LocalizationPath { get; set; } = "Locales";
}

/// <summary>
/// Supported JSON-LD entity types for the report page.
/// </summary>
public enum JsonLdType
{
    /// <summary>
    /// Use schema.org/Article.
    /// </summary>
    Article = 0,

    /// <summary>
    /// Use schema.org/Dataset (for richer structured metric descriptions).
    /// </summary>
    Dataset = 1
}
