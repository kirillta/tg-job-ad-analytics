# TgJobAdAnalytics

An analytics service for analyzing job advertisements from Telegram channels. The application extracts and processes job postings, detects salary information using AI-powered pattern matching, normalizes currencies, and generates statistical reports.

## Features

- 📊 **Job Ad Analysis**: Extract and process job postings from Telegram channels
- 💰 **AI-Powered Salary Detection**: Multi-currency salary extraction with intelligent pattern matching
- 🔄 **Currency Normalization**: Automatic currency conversion with historical exchange rates
- 📈 **Statistical Reports**: Generate comprehensive salary trend reports with interactive charts
- 🔍 **Duplicate Detection**: Uses locality-sensitive hashing (LSH) to identify similar messages
- ⚡ **High Performance**: Parallel processing with adaptive rate limiting (5-10x faster)
- 🎯 **Position Level Detection**: Automatically categorizes job positions (Junior, Middle, Senior, etc.)

## Technologies

- **.NET 9** - Latest .NET platform
- **Entity Framework Core** - Data access with SQLite
- **OpenAI API** - AI-powered salary and position level extraction  
- **MathNet.Numerics** - Statistical operations
- **Scriban** - HTML templating for reports
- **Xunit & NSubstitute** - Testing framework

---

## Calculation Methodology

1.	**Ingestion:** Messages are collected from monitored Telegram chats and persisted with their original UTC timestamps.
2.	**Extraction:** Each message is parsed; job ads are identified and salary mentions extracted via pattern matching (lower/upper bounds, currency, and period).
3.	**Normalization:** Currencies and periods are normalized to a common base; numeric ranges are stored (lower/upper) together with a normalized midpoint when needed.
4.	**De‑duplication:** Similar ads are detected (locality-sensitive hashing / text similarity) and flagged so unique-ad statistics exclude repeats.
5.	**Validation & Filtering:** Invalid or ambiguous salary parses are discarded; statistics exclude incomplete current-month data.
6.	**Aggregation:** Metrics (counts, unique counts, salary distributions, averages, etc.) are computed per grouping interval and per source.
7.	**Reporting:** Values are formatted and charts rendered. The reporting window ends on the last day of the previous month to avoid partial intervals.

---

## Getting Started

### Prerequisites

- .NET 9 SDK
- OpenAI API key (set as environment variable `PNKL_OPEN_AI_KEY`)
- SQLite database

### Configuration

Configure the application in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=path\\to\\Analytics.db"
  },
  "Upload": {
    "Mode": "Skip",
    "BatchSize": 10000,
    "SalaryExtractionChunkSize": 1000,
    "SalaryExtractionInitialConcurrency": 5,
    "SalaryExtractionMaxConcurrency": 20,
    "AdaptiveThrottleSuccessThreshold": 0.95,
    "AdaptiveThrottleWindowSize": "00:01:00"
  }
}
```

### Running the Application

```bash
dotnet run --project src/TgJobAdAnalytics
```

---

## Salary Extraction Performance Optimization

The salary extraction pipeline has been optimized for high-performance parallel processing with intelligent adaptive rate limiting.

### Performance Improvements

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Throughput** | 2-5 ads/sec | 15-40 ads/sec | **5-10x faster** |
| **Memory Usage** | ~250MB (50K ads) | ~5-6MB per chunk | **95% reduction** |
| **DB Queries** | 20,000+ | 20-30 | **99% reduction** |
| **Processing Time** | 30-80 min (10K ads) | 5-15 min | **5-6x faster** |

### Key Features

✅ **Adaptive Rate Limiting** - Dynamically adjusts concurrency based on API response rates  
✅ **Memory-Efficient Streaming** - Processes ads in configurable chunks using `IAsyncEnumerable`  
✅ **Batch Database Operations** - Eliminates N+1 query problems with intelligent preloading  
✅ **Resilient Error Handling** - Automatic retry with exponential backoff for transient failures  
✅ **Circuit Breaker Pattern** - Automatically reduces load after consecutive failures  
✅ **Comprehensive Monitoring** - Structured logging for performance tracking and debugging

### Architecture

The optimization uses a three-stage pipeline:

```
┌─────────────────┐
│  Stream Chunks  │  ← Loads 1000 ads at a time
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Preload Tags   │  ← Single DB query per chunk
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Parallel Process│  ← 5-20 concurrent API calls
│  (Adaptive)     │    (adjusts dynamically)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Batch Persist  │  ← Batched database writes
└─────────────────┘
```

---

## Configuration Parameters

### Primary Configuration

These are the main parameters that control the salary extraction pipeline:

### SalaryExtractionChunkSize (default: 1000)

**What it controls:** Number of ads loaded into memory and processed together before moving to the next chunk.

**Memory impact:** `ChunkSize × ~5KB per AdEntity + tags dictionary`
- 500 ads ≈ 2.5MB per chunk
- 1000 ads ≈ 5MB per chunk  
- 2000 ads ≈ 10MB per chunk

**Recommended values:**
- Small dataset (<5K ads): `500`
- Medium dataset (5K-50K ads): `1000`
- Large dataset (>50K ads): `1500-2000`

### SalaryExtractionInitialConcurrency (default: 5)

**What it controls:** Starting number of parallel API calls when processing begins.

**Recommended values:**
- Conservative: `3-5`
- Standard: `5-8`
- Aggressive: `10-15` (only if API supports it)

### SalaryExtractionMaxConcurrency (default: 20)

**What it controls:** Maximum number of parallel API calls the adaptive limiter can scale up to.

⚠️ **Important:** This should match or be below your API's actual rate limit!

**Recommended values:**
- OpenAI free tier: `3-5`
- OpenAI paid tier: `10-20`
- Custom API: Check documentation

### AdaptiveThrottleSuccessThreshold (default: 0.95)

**What it controls:** Success rate percentage (0.0-1.0) needed before increasing concurrency.

**Recommended values:**
- Production: `0.95-0.97`
- Testing: `0.90`
- Unstable API: `0.98`

### AdaptiveThrottleWindowSize (default: 00:01:00)

**What it controls:** Time window for calculating success rates.

**Recommended values:**
- Fast-changing conditions: `00:00:30`
- Standard: `00:01:00`
- Stable conditions: `00:02:00`

---

### Advanced Rate Limiter Configuration

These parameters control the internal behavior of the adaptive rate limiter. Most users should use the defaults.

#### CircuitBreakerFailureThreshold (default: 5)
Number of consecutive failures before triggering aggressive concurrency reduction.

#### ConcurrencyIncrement (default: 1)
Amount to increase concurrency by when performance is good.

#### ConcurrencyDecreaseRatio (default: 0.3)
Ratio (0.0-1.0) by which to decrease concurrency on failures (30%).

#### LowSuccessRateMultiplier (default: 0.9)
Multiplier for success threshold (90%) to determine when to decrease concurrency.

#### MinimumConcurrency (default: 1)
Lowest allowed concurrency level.

#### MinimumConcurrencyDecrement (default: 1)
Minimum amount to decrease concurrency by.

#### MinimumSampleSize (default: 10)
Minimum number of samples required before adjusting concurrency.

**Example advanced configuration:**
```json
{
  "Upload": {
    "AdaptiveRateLimiter": {
      "CircuitBreakerFailureThreshold": 3,
      "ConcurrencyDecreaseRatio": 0.5,
      "MinimumSampleSize": 20
    }
  }
}
```

---

## Performance Tuning Scenarios

### Scenario 1: Getting Rate Limited Frequently

**Symptoms:** Logs show many "Rate limit error: true" messages, frequent concurrency decreases

**Solutions:**
```json
{
  "SalaryExtractionInitialConcurrency": 3,
  "SalaryExtractionMaxConcurrency": 10,
  "AdaptiveThrottleSuccessThreshold": 0.97
}
```

### Scenario 2: Too Slow, Not Using Full Capacity

**Symptoms:** Concurrency stays low, no rate limit errors, slow throughput

**Solutions:**
```json
{
  "SalaryExtractionInitialConcurrency": 10,
  "SalaryExtractionMaxConcurrency": 30,
  "AdaptiveThrottleSuccessThreshold": 0.92
}
```

### Scenario 3: Memory Pressure / Out of Memory

**Symptoms:** Application crashes or slows down, high memory usage

**Solutions:**
```json
{
  "SalaryExtractionChunkSize": 500,
  "SalaryExtractionMaxConcurrency": 10
}
```

### Scenario 4: Unstable Concurrency

**Symptoms:** Logs show frequent increases/decreases in concurrency

**Solutions:**
```json
{
  "SalaryExtractionInitialConcurrency": 8,
  "SalaryExtractionMaxConcurrency": 12,
  "AdaptiveThrottleSuccessThreshold": 0.96,
  "AdaptiveThrottleWindowSize": "00:02:00"
}
```

---

## Monitoring & Observability

### Key Log Messages to Watch

1. **"Processing chunk of X ads with concurrency: Y"**
   - Monitor Y over time - should be stable or gradually increasing
   
2. **"Preloaded X message tags in Yms"**
   - Should be < 200ms per chunk
   - If > 500ms, consider decreasing chunk size or database optimization

3. **"Adaptive limiter: increased/decreased concurrency"**
   - Occasional adjustments are normal
   - Frequent adjustments indicate instability

4. **"Failed to process ad {AdId}. Rate limit error: true"**
   - Should be rare (< 1% of requests)
   - If frequent, reduce max concurrency

### Performance Calculations

**Expected throughput:**
```
Throughput (ads/sec) ≈ AvgConcurrency / AvgApiResponseTime

Example:
- 10 concurrent calls, 0.5 second avg API response
= 10 / 0.5 = 20 ads/second = 1,200 ads/minute
```

**Estimated completion time:**
```
Time = TotalAds / (AvgConcurrency / AvgApiResponseTime)

Example:
- 10,000 ads, 8 concurrent calls, 0.4s avg response
= 10,000 / (8 / 0.4) = 500 seconds ≈ 8 minutes
```

---

## Optimal Configurations by Use Case

### Development / Testing (Small Dataset)
```json
{
  "SalaryExtractionChunkSize": 100,
  "SalaryExtractionInitialConcurrency": 2,
  "SalaryExtractionMaxConcurrency": 5,
  "AdaptiveThrottleSuccessThreshold": 0.95,
  "AdaptiveThrottleWindowSize": "00:00:30"
}
```

### Production (OpenAI Paid Tier)
```json
{
  "SalaryExtractionChunkSize": 1000,
  "SalaryExtractionInitialConcurrency": 8,
  "SalaryExtractionMaxConcurrency": 20,
  "AdaptiveThrottleSuccessThreshold": 0.95,
  "AdaptiveThrottleWindowSize": "00:01:00"
}
```

### High-Volume Production (Custom API)
```json
{
  "SalaryExtractionChunkSize": 2000,
  "SalaryExtractionInitialConcurrency": 15,
  "SalaryExtractionMaxConcurrency": 50,
  "AdaptiveThrottleSuccessThreshold": 0.93,
  "AdaptiveThrottleWindowSize": "00:01:30"
}
```

### Conservative (Shared API Key)
```json
{
  "SalaryExtractionChunkSize": 500,
  "SalaryExtractionInitialConcurrency": 3,
  "SalaryExtractionMaxConcurrency": 8,
  "AdaptiveThrottleSuccessThreshold": 0.97,
  "AdaptiveThrottleWindowSize": "00:02:00"
}
```

---

## Troubleshooting

### Checklist

- [ ] Check API rate limits in provider documentation
- [ ] Verify database connection pool size ≥ max concurrency
- [ ] Monitor memory usage during execution
- [ ] Review logs for error patterns
- [ ] Test with small dataset before full run
- [ ] Adjust one parameter at a time
- [ ] Document changes and their effects

### Advanced Tips

1. **Start Conservative**: Begin with low values and increase gradually
2. **One Change at a Time**: Only adjust one parameter per test run
3. **Log Analysis**: Grep logs for "Adaptive limiter" to see behavior patterns
4. **A/B Testing**: Try different configs on same dataset to compare
5. **Peak Hours**: API performance may vary by time of day
6. **Burst vs Sustained**: Short bursts can handle higher concurrency than sustained load

---

## Technical Implementation Details

### Memory Optimization - Streaming Architecture

- **Before**: Loaded ALL ads into memory at once (`List<AdEntity>`)
- **After**: Streaming with `IAsyncEnumerable<AdEntity>` using `[EnumeratorCancellation]`
- **Memory savings**: From O(n) to O(concurrency) - approximately 250MB → 50KB for 50,000 ads

### Database Query Optimization

- **Eliminated N+1 query problem**: Was calling `GetMessageTags()` once per ad
- **Batch preloading**: Single query per chunk to load all message tags
- **Query optimization**: Changed from client-side filtering to SQL-level `NOT EXISTS` subquery
- **Result**: Reduced database queries from 20,000+ to ~20-30 total

### Parallel Processing Implementation

**Per-chunk workflow:**
1. Stream chunk from database using `IAsyncEnumerable`
2. Preload all message tags for the chunk (single query)
3. Process ads in parallel using `Parallel.ForEachAsync` with dynamic concurrency
4. Write results to bounded channel for batched persistence

### Error Handling & Resilience

- **Per-ad error isolation**: Failures don't stop the entire batch
- **Rate limit detection**: Identifies rate limit errors from exception messages (429, "rate limit", "too many requests")
- **Automatic backoff**: 2-second delay on rate limit errors
- **Adaptive recovery**: Automatically reduces concurrency by 30% and recovers
- **Circuit breaker**: Triggers after 5 consecutive failures
- **Comprehensive logging**: Tracks failures with ad IDs, error types, and concurrency adjustments

### Adaptive Rate Limiter

The `AdaptiveRateLimiter` class provides intelligent concurrency management:

- **Success tracking**: Maintains sliding window of operation results (configurable time window)
- **Dynamic scaling**: Increases concurrency by 1 when success rate > threshold (default 95%)
- **Aggressive backoff**: Decreases concurrency by 30% on rate limit errors
- **Thread-safe**: Uses proper locking for concurrent access
- **Minimal sample size**: Requires 10+ results before adjusting (prevents unstable behavior)
- **Semaphore-based**: Uses `SemaphoreSlim` for efficient permit management

---

## Copy Latest Report to `dist`

A PowerShell script is provided to copy the latest generated report (all locales) into a deployable `dist` folder at the solution root.

### What It Does

- Finds the newest run under `Output/<runFolder>` (prefers `yyyyMMdd-HHmmssZ` naming; falls back to latest by write time).
- Copies every locale's `index.html` into `dist/<locale>/index.html`.
- Creates `dist` if missing and clears old contents (preserves `.gitkeep` if present).

### Run from Solution Root

**Windows PowerShell:**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\dist.ps1
```

**PowerShell 7 (cross-platform):**
```powershell
pwsh -NoProfile -File ./dist.ps1
```

### Parameters

- `-OutputRoot`: path to the output folder (default: ./Output)
- `-DistRoot`: path to the dist folder (default: ./dist)

### Examples

```powershell
powershell -NoProfile -File ./dist.ps1 -OutputRoot ./output -DistRoot ./dist
```

### Visual Studio (Optional)

Tools > External Tools > Add:
- **Title**: Copy Latest Report
- **Command**: powershell.exe
- **Arguments**: `-NoProfile -ExecutionPolicy Bypass -File $(SolutionDir)dist.ps1`
- **Initial directory**: `$(SolutionDir)`

### Notes

- The script always copies all locales found in the latest run.
- Exits 0 on success; non-zero otherwise.

---

## Project Structure

```
TgJobAdAnalytics/
├── src/
│   └── TgJobAdAnalytics/
│       ├── Data/                      # Entity models and DbContext
│       ├── Models/                    # Domain models and DTOs
│       ├── Services/
│       │   ├── Salaries/              # Salary extraction pipeline
│       │   │   ├── SalaryExtractionProcessor.cs
│       │   │   ├── AdaptiveRateLimiter.cs
│       │   │   └── SalaryPersistenceService.cs
│       │   ├── Levels/                # Position level detection
│       │   ├── Reports/               # Report generation
│       │   └── Vectors/               # Similarity detection (LSH)
│       └── Utils/                     # Utility classes
├── tests/
│   └── Tests/                         # Unit and integration tests
└── docs/                              # Documentation
```

---

## Testing

Run tests with:

```bash
dotnet test
```

The test suite includes:
- Unit tests for core business logic
- Integration tests for database operations
- Performance benchmarks

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the code style guidelines in `.github/copilot-instructions.md`
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

### Code Style Guidelines

- Line length: 160 characters
- Two blank lines between methods
- One blank line between properties
- Private fields at the end of the file
- Use XML documentation for public members
- Always use `DateTime.UtcNow` instead of `DateTime.Now`
- Use named parameters for clarity

---

## Additional Resources

- [Salary Extraction Optimization Details](docs/salary-extraction-optimization.md) - Complete implementation summary
- [Performance Tuning Guide](docs/salary-extraction-tuning-guide.md) - Detailed tuning scenarios and troubleshooting
- [Code Style Guidelines](.github/copilot-instructions.md) - Project coding standards

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

## Acknowledgments

- OpenAI for AI-powered extraction capabilities
- The .NET community for excellent libraries and tools
- Contributors who help improve this project

---

**Built with ❤️ using .NET 9**