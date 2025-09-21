using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Reports.Metadata;

namespace TgJobAdAnalytics.Services.Localization;

/// <summary>
/// Simple in-memory localization provider loading per-locale JSON dictionaries from a configured folder.
/// </summary>
public sealed class InMemoryLocalizationProvider : ILocalizationProvider
{
    public InMemoryLocalizationProvider(IOptions<SiteMetadataOptions> siteOptions, ILogger<InMemoryLocalizationProvider> logger)
    {
        _logger = logger;
        _siteOptions = siteOptions.Value;
        _resourceRoot = ResolvePath(_siteOptions.LocalizationPath);
        LoadResources();
    }


    public string Get(string locale, string key)
    {
        locale = Normalize(locale);
        if (!_resources.TryGetValue(locale, out var dict))
            throw new InvalidOperationException($"Locale '{locale}' not loaded.");

        if (!dict.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing localization key '{key}' for locale '{locale}'.");

        return value;
    }


    private void LoadResources()
    {
        foreach (var locale in _siteOptions.Locales)
        {
            try
            {
                var file = Path.Combine(_resourceRoot, $"{locale}.json");
                if (!File.Exists(file))
                {
                    _logger.LogWarning("Localization file not found for locale {Locale} at {Path}", locale, file);
                    continue;
                }

                var json = File.ReadAllText(file);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                _resources[Normalize(locale)] = dict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load localization for {Locale}", locale);
                throw;
            }
        }
    }


    private static string Normalize(string locale) => locale.Trim().Replace('_', '-');


    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(AppContext.BaseDirectory, path);
    }


    private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryLocalizationProvider> _logger;
    private readonly SiteMetadataOptions _siteOptions;
    private readonly string _resourceRoot;
}
