namespace TgJobAdAnalytics.Utils;

/// <summary>
/// Fixed namespaces for deterministic GUID generation across entity types.
/// </summary>
public static class Namespaces
{
    // Arbitrary but constant values. Do not change once in use.
    public static readonly Guid Messages = new("6b3a1bcd-9c3c-4a68-9b7a-8e2c23c7f3a1");
    public static readonly Guid Ads = new("2f8c6b8d-1c2e-4e0a-9f4b-5d6e7f8a9b0c");
}
