using System.Text.Json;
using Microsoft.Extensions.Options;
using TgJobAdAnalytics.Models.Reports.Metadata;

namespace TgJobAdAnalytics.Services.Localization;

/// <summary>
/// Simple in-memory localization provider loading per-locale JSON dictionaries from a configured folder.
/// </summary>
public sealed class InMemoryLocalizationProvider : ILocalizationProvider
{
    public InMemoryLocalizationProvider(IOptions<SiteMetadataOptions> siteOptions)
    {
        _siteOptions = siteOptions.Value;
        _resourceRoot = ResolvePath(_siteOptions.LocalizationPath);

        LoadResources();
    }


    public string Get(string locale, string key)
    {
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
            var file = Path.Combine(_resourceRoot, $"{locale}.json");
            if (!File.Exists(file))
                throw new InvalidOperationException($"Localization file for locale '{locale}' not found at path '{file}'.");

            var json = File.ReadAllText(file);
            _resources[locale] = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
    }


    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(AppContext.BaseDirectory, path);
    }


    private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase);
    
    private readonly string _resourceRoot;
    private readonly SiteMetadataOptions _siteOptions;
}
