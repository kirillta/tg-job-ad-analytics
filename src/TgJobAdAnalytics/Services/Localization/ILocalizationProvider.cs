namespace TgJobAdAnalytics.Services.Localization;

/// <summary>
/// Provides localized string values for a given locale and key. Fails fast if a key is missing.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>
    /// Retrieves a localized string for the specified locale and resource key.
    /// </summary>
    /// <param name="locale">Locale identifier (e.g. "en", "ru").</param>
    /// <param name="key">Resource key to resolve.</param>
    /// <returns>Localized string value.</returns>
    string Get(string locale, string key);
}
