using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Models.OpenAI;

namespace TgJobAdAnalytics.Utils;

/// <summary>
/// Adaptive rate limiter that dynamically adjusts concurrency based on success rates and response times.
/// </summary>
public sealed class AdaptiveRateLimiter : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRateLimiter"/> class.
    /// </summary>
    /// <param name="openAiOptions">OpenAI API configuration options.</param>
    public AdaptiveRateLimiter( OpenAiOptions openAiOptions)
    {
        _maxConcurrency = openAiOptions.MaxConcurrency;
        _successThreshold = openAiOptions.AdaptiveThrottleSuccessThreshold;
        _windowSize = openAiOptions.AdaptiveThrottleWindowSize;
        _currentConcurrency = openAiOptions.InitialConcurrency;
        
        _semaphore = new SemaphoreSlim(_currentConcurrency, _maxConcurrency);
        _results = new Queue<(DateTime timestamp, bool success)>();

        _rateLimiterOptions = openAiOptions.AdaptiveRateLimiter;
    }


    /// <summary>
    /// Gets the current concurrency level.
    /// </summary>
    public int CurrentConcurrency 
        => _currentConcurrency;


    /// <summary>
    /// Acquires a permit to execute an operation. Must be disposed to release the permit.
    /// Honors any active server-provided backoff window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the acquisition.</param>
    /// <returns>A disposable token that releases the permit when disposed.</returns>
    public async Task<IDisposable> Acquire(CancellationToken cancellationToken)
    {
        while (true)
        {
            var holdUntilTicks = Interlocked.Read(ref _holdUntilTicks);
            if (holdUntilTicks > 0)
            {
                var remaining = new DateTime(holdUntilTicks, DateTimeKind.Utc) - DateTime.UtcNow;
                remaining = remaining + _minBackoff;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                    continue;
                }
            }

            await _semaphore.WaitAsync(cancellationToken);
            Interlocked.Increment(ref _inUse);

            var recheckTicks = Interlocked.Read(ref _holdUntilTicks);
            if (recheckTicks > 0)
            {
                var remaining = new DateTime(recheckTicks, DateTimeKind.Utc) - DateTime.UtcNow;
                remaining = remaining + _minBackoff;
                if (remaining > TimeSpan.Zero)
                {
                    Interlocked.Decrement(ref _inUse);
                    _semaphore.Release();

                    await Task.Delay(remaining, cancellationToken);
                    continue;
                }
            }

            return new ReleaseToken(this);
        }
    }


    /// <summary>
    /// Records a successful operation and adjusts concurrency if needed.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _results.Enqueue((DateTime.UtcNow, true));
            _consecutiveFailures = 0;
            AdjustConcurrency();
        }
    }


    /// <summary>
    /// Records a failed operation and adjusts concurrency if needed.
    /// </summary>
    /// <param name="isRateLimitError">Indicates whether the failure was due to a rate limit error.</param>
    public void RecordFailure(bool isRateLimitError)
    {
        lock (_lock)
        {
            _results.Enqueue((DateTime.UtcNow, false));
            _consecutiveFailures++;

            if (isRateLimitError || _consecutiveFailures >= _rateLimiterOptions.CircuitBreakerFailureThreshold)
                DecreaseConcurrency();
            else
                AdjustConcurrency();
        }
    }


    /// <summary>
    /// Records a server-provided retry-after backoff to temporarily pause new acquisitions.
    /// Also decreases concurrency proactively.
    /// </summary>
    /// <param name="delay">The retry delay indicated by the server.</param>
    public void RecordRetryAfter(in TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
            return;

        // Set the global hold-until to the max seen so far.
        var untilTicks = (DateTime.UtcNow + delay).Ticks;
        long observed;
        do
        {
            observed = Interlocked.Read(ref _holdUntilTicks);
            if (untilTicks <= observed)
                break;
        }
        while (Interlocked.CompareExchange(ref _holdUntilTicks, untilTicks, observed) != observed);

        lock (_lock)
            DecreaseConcurrency();
    }


    /// <summary>
    /// Releases all resources used by the rate limiter.
    /// </summary>
    public void Dispose() 
        => _semaphore.Dispose();


    private void AdjustConcurrency()
    {
        CleanupOldResults();

        if (_results.Count < _rateLimiterOptions.MinimumSampleSize)
            return;

        var successCount = _results.Count(r => r.success);
        var successRate = (double)successCount / _results.Count;

        if (successRate >= _successThreshold && _currentConcurrency < _maxConcurrency)
            IncreaseConcurrency();
        else if (successRate < _successThreshold * _rateLimiterOptions.LowSuccessRateMultiplier)
            DecreaseConcurrency();
    }


    private void IncreaseConcurrency()
    {
        var oldConcurrency = _currentConcurrency;
        var increment = Math.Min(_rateLimiterOptions.ConcurrencyIncrement, _maxConcurrency - _currentConcurrency);
        if (increment <= 0) 
            return;

        _currentConcurrency += increment;

        var desiredAvailable = Math.Max(0, _currentConcurrency - Volatile.Read(ref _inUse));
        var toRelease = Math.Max(0, desiredAvailable - _semaphore.CurrentCount);
        if (toRelease > 0)
            _semaphore.Release(toRelease);
    }


    private void DecreaseConcurrency()
    {
        var oldConcurrency = _currentConcurrency;
        var decrement = Math.Max(_rateLimiterOptions.MinimumConcurrencyDecrement, (int)Math.Ceiling(_currentConcurrency * _rateLimiterOptions.ConcurrencyDecreaseRatio));
        _currentConcurrency = Math.Max(_rateLimiterOptions.MinimumConcurrency, _currentConcurrency - decrement);

        var desiredAvailable = Math.Max(0, _currentConcurrency - Volatile.Read(ref _inUse));
        var excess = _semaphore.CurrentCount - desiredAvailable;
        while (excess > 0 && _semaphore.Wait(0))
            excess--;
    }


    private void CleanupOldResults()
    {
        var cutoff = DateTime.UtcNow - _windowSize;
        while (_results.Count > 0 && _results.Peek().timestamp < cutoff)
            _results.Dequeue();
    }


    private sealed class ReleaseToken : IDisposable
    {
        public ReleaseToken(AdaptiveRateLimiter limiter) 
            => _limiter = limiter;


        public void Dispose()
        {
            Interlocked.Decrement(ref _limiter._inUse);
            _limiter._semaphore.Release();
        }

        
        private readonly AdaptiveRateLimiter _limiter;
    }
    

    private int _consecutiveFailures;
    private int _currentConcurrency;
    private int _inUse;
    private long _holdUntilTicks;
    private readonly Lock _lock = new();
    private readonly int _maxConcurrency;
    private static readonly TimeSpan _minBackoff = TimeSpan.FromSeconds(1);
    private readonly Queue<(DateTime timestamp, bool success)> _results;
    private readonly SemaphoreSlim _semaphore;
    private readonly double _successThreshold;
    private readonly TimeSpan _windowSize;

    private readonly AdaptiveRateLimiterOptions _rateLimiterOptions;
}
