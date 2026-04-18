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

    /// <summary>
    /// Attempts to retrieve a localized string for the specified locale and resource key.
    /// </summary>
    /// <param name="locale">Locale identifier.</param>
    /// <param name="key">Resource key to resolve.</param>
    /// <param name="value">Localized value if found; otherwise an empty string.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    bool TryGet(string locale, string key, out string value);
}
