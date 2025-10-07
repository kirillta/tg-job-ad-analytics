using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Models.OpenAI;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Adaptive rate limiter that dynamically adjusts concurrency based on success rates and response times.
/// </summary>
public sealed class AdaptiveRateLimiter : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRateLimiter"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <param name="openAiOptions">OpenAI API configuration options.</param>
    public AdaptiveRateLimiter(ILoggerFactory loggerFactory, OpenAiOptions openAiOptions)
    {
        _logger = loggerFactory.CreateLogger<AdaptiveRateLimiter>();

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
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the acquisition.</param>
    /// <returns>A disposable token that releases the permit when disposed.</returns>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new ReleaseToken(this);
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
        _currentConcurrency = Math.Min(_currentConcurrency + _rateLimiterOptions.ConcurrencyIncrement, _maxConcurrency);

        if (_currentConcurrency > oldConcurrency)
        {
            _semaphore.Release();
            _logger.LogInformation("Adaptive limiter: increased concurrency from {OldConcurrency} to {NewConcurrency}", oldConcurrency, _currentConcurrency);
        }
    }


    private void DecreaseConcurrency()
    {
        var oldConcurrency = _currentConcurrency;
        var decrease = Math.Max(_rateLimiterOptions.MinimumConcurrencyDecrement, (int)(_currentConcurrency * _rateLimiterOptions.ConcurrencyDecreaseRatio));
        _currentConcurrency = Math.Max(_rateLimiterOptions.MinimumConcurrency, _currentConcurrency - decrease);

        _logger.LogInformation("Adaptive limiter: decreased concurrency from {OldConcurrency} to {NewConcurrency}", oldConcurrency, _currentConcurrency);
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
        {
            _limiter = limiter;
        }


        public void Dispose() 
            => _limiter._semaphore.Release();


        private readonly AdaptiveRateLimiter _limiter;
    }


    private int _consecutiveFailures;
    private int _currentConcurrency;
    private readonly int _maxConcurrency;
    private readonly Queue<(DateTime timestamp, bool success)> _results;
    private readonly SemaphoreSlim _semaphore;
    private readonly double _successThreshold;
    private readonly TimeSpan _windowSize;

    
    private readonly Lock _lock = new();

    private readonly ILogger<AdaptiveRateLimiter> _logger;
    private readonly AdaptiveRateLimiterOptions _rateLimiterOptions;
}
