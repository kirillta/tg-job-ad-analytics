namespace TgJobAdAnalytics.Models.Stacks;

/// <summary>
/// Options for channel->stack mapping file location.
/// </summary>
public sealed class StackMappingOptions
{
    /// <summary>
    /// Gets or sets the path to the mapping JSON file. Can be relative to base directory.
    /// </summary>
    public string MappingFilePath { get; set; } = string.Empty;
}
