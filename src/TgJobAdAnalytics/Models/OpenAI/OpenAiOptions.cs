namespace TgJobAdAnalytics.Models.OpenAI;

/// <summary>
/// Configuration options for OpenAI API integration.
/// </summary>
public class OpenAiOptions
{
    /// <summary>
    /// Gets or sets the chunk size for streaming data during OpenAI processing. Affects memory usage and database query batching.
    /// </summary>
    public int ProcessingChunkSize { get; set; } = 1000;


    /// <summary>
    /// Gets or sets the initial concurrency level for parallel OpenAI API calls.
    /// </summary>
    public int InitialConcurrency { get; set; } = 5;


    /// <summary>
    /// Gets or sets the maximum concurrency level for parallel OpenAI API calls.
    /// </summary>
    public int MaxConcurrency { get; set; } = 20;


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
