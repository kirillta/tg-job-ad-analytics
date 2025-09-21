namespace TgJobAdAnalytics.Services.Localization;

/// <summary>
/// Provides localized string values for a given locale and key. Fails fast if a key is missing.
/// </summary>
public interface ILocalizationProvider
{
    string Get(string locale, string key);
}
