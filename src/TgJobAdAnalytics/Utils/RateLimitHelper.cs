using System.Text.RegularExpressions;

namespace TgJobAdAnalytics.Utils;

/// <summary>
/// Provides static helpers for detecting and parsing OpenAI rate-limit errors.
/// </summary>
internal static partial class RateLimitHelper
{
    /// <summary>
    /// Returns <c>true</c> when the exception indicates an OpenAI rate-limit (HTTP 429) response.
    /// </summary>
    public static bool IsRateLimitException(Exception ex)
    {
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("rate_limit_exceeded") 
            || message.Contains("429") 
            || ex.GetType().Name.Contains("RateLimit", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Returns <c>true</c> when the exception indicates an OpenAI quota-exceeded error.
    /// </summary>
    public static bool IsQuotaExceeded(Exception ex)
    {
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("insufficient_quota") || message.Contains("quota") && message.Contains("exceeded");
    }


    /// <summary>
    /// Attempts to parse a retry-after delay from the exception message ("try again in N ms/s").
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <param name="delay">The parsed delay, clamped to 50 ms–30 s.</param>
    /// <returns><c>true</c> if a valid delay was found; otherwise <c>false</c>.</returns>
    public static bool TryParseRetryAfter(Exception ex, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        var errorMessage = ex.Message ?? string.Empty;

        var match = RateLimitRetryRegex().Match(errorMessage);
        if (!match.Success)
            return false;

        var retryAfterValue = int.Parse(match.Groups["val"].Value);
        var unit = match.Groups["unit"].Value.ToLowerInvariant();

        delay = unit switch
        {
            "ms" => TimeSpan.FromMilliseconds(retryAfterValue),
            "s" or "sec" or "secs" or "second" or "seconds" => TimeSpan.FromSeconds(retryAfterValue),
            _ => TimeSpan.Zero
        };

        if (delay <= TimeSpan.Zero)
            return false;

        if (delay < TimeSpan.FromMilliseconds(50))
            delay = TimeSpan.FromMilliseconds(50);

        if (delay > TimeSpan.FromSeconds(30))
            delay = TimeSpan.FromSeconds(30);

        return true;
    }


    [GeneratedRegex(@"try again in\s+(?<val>\d+)\s*(?<unit>ms|s|sec|secs|second|seconds)", RegexOptions.IgnoreCase)]
    private static partial Regex RateLimitRetryRegex();
}
