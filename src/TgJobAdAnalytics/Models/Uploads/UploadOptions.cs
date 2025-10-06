using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Services.Uploads;

namespace TgJobAdAnalytics.Models.Uploads;

/// <summary>
/// Configuration options for the <see cref="TelegramChatImportService"/>.
/// </summary>
public class UploadOptions
{
    /// <summary>
    /// Gets or sets the batch size for message uploads. Larger values can improve performance but require more memory.
    /// </summary>
    public int BatchSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the mode of operation for the upload process.
    /// </summary>
    public UploadMode Mode { get; set; } = UploadMode.OnlyNewMessages;

    /// <summary>
    /// Specifies the path to the source directory containing the data to be uploaded.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chunk size for streaming ads during salary extraction. Affects memory usage and database query batching.
    /// </summary>
    public int SalaryExtractionChunkSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the initial concurrency level for parallel salary extraction API calls.
    /// </summary>
    public int SalaryExtractionInitialConcurrency { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum concurrency level for parallel salary extraction API calls.
    /// </summary>
    public int SalaryExtractionMaxConcurrency { get; set; } = 20;

    /// <summary>
    /// Gets or sets the success rate threshold for adaptive throttling. When success rate exceeds this value, concurrency may be increased.
    /// </summary>
    public double AdaptiveThrottleSuccessThreshold { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the time window size for calculating success rate in adaptive throttling.
    /// </summary>
    public TimeSpan AdaptiveThrottleWindowSize { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the adaptive rate limiter configuration options.
    /// </summary>
    public AdaptiveRateLimiterOptions AdaptiveRateLimiter { get; set; } = new();
}
