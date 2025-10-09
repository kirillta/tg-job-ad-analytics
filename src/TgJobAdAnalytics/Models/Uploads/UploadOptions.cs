using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Uploads;

namespace TgJobAdAnalytics.Models.Uploads;

/// <summary>
/// Configuration options for the <see cref="TelegramChatImportService"/>.
/// </summary>
public sealed class UploadOptions
{
    /// <summary>
    /// Gets or sets the batch size for message uploads. Larger values can improve performance but require more memory.
    /// </summary>
    public int BatchSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the mode of operation for the upload process.
    /// </summary>
    public UploadMode Mode { get; set; } = UploadMode.Append;

    /// <summary>
    /// Specifies the path to the source directory containing the data to be uploaded.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
}
