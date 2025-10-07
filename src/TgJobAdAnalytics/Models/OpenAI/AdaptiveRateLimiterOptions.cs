namespace TgJobAdAnalytics.Models.OpenAI;

/// <summary>
/// Configuration options for the adaptive rate limiter behavior.
/// </summary>
public class AdaptiveRateLimiterOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before triggering circuit breaker behavior.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;


    /// <summary>
    /// Gets or sets the amount by which to increase concurrency when conditions are favorable.
    /// </summary>
    public int ConcurrencyIncrement { get; set; } = 1;


    /// <summary>
    /// Gets or sets the ratio (0.0-1.0) by which to decrease concurrency on failures.
    /// </summary>
    public double ConcurrencyDecreaseRatio { get; set; } = 0.3;


    /// <summary>
    /// Gets or sets the multiplier for success threshold to determine when to decrease concurrency.
    /// </summary>
    public double LowSuccessRateMultiplier { get; set; } = 0.9;


    /// <summary>
    /// Gets or sets the minimum allowed concurrency level.
    /// </summary>
    public int MinimumConcurrency { get; set; } = 1;


    /// <summary>
    /// Gets or sets the minimum amount to decrease concurrency by.
    /// </summary>
    public int MinimumConcurrencyDecrement { get; set; } = 1;


    /// <summary>
    /// Gets or sets the minimum number of samples required before adjusting concurrency.
    /// </summary>
    public int MinimumSampleSize { get; set; } = 10;
}
