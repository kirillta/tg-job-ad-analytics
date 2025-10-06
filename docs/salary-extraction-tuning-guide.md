# Salary Extraction Performance Tuning Guide

## Overview
This guide helps you tune the salary extraction parallelization parameters based on your specific API rate limits and performance requirements.

## Configuration Parameters

### SalaryExtractionChunkSize (default: 1000)
**What it controls:** Number of ads loaded into memory and processed together before moving to the next chunk.

**Memory impact:** `ChunkSize × ~5KB per AdEntity + tags dictionary`
- 500 ads ? 2.5MB per chunk
- 1000 ads ? 5MB per chunk  
- 2000 ads ? 10MB per chunk

**When to adjust:**
- **Increase** if you have plenty of memory and want fewer database queries
- **Decrease** if experiencing memory pressure or want more frequent progress updates

**Recommended values:**
- Small dataset (<5K ads): 500
- Medium dataset (5K-50K ads): 1000
- Large dataset (>50K ads): 1500-2000

---

### SalaryExtractionInitialConcurrency (default: 5)
**What it controls:** Starting number of parallel API calls when processing begins.

**Risk:** Too high can immediately hit rate limits; too low wastes throughput potential.

**When to adjust:**
- **Increase** if you know your API can handle more concurrent requests
- **Decrease** if seeing rate limit errors at startup

**Recommended values:**
- Conservative: 3-5
- Standard: 5-8
- Aggressive: 10-15 (only if API supports it)

---

### SalaryExtractionMaxConcurrency (default: 20)
**What it controls:** Maximum number of parallel API calls the adaptive limiter can scale up to.

**Important:** This should match or be below your API's actual rate limit!

**When to adjust:**
- **Set based on API limits:** If API allows 10 req/sec, set max to 8-10
- **Database connections:** Ensure connection pool ? max concurrency

**Recommended values:**
- OpenAI free tier: 3-5
- OpenAI paid tier: 10-20
- Custom API: Check documentation

---

### AdaptiveThrottleSuccessThreshold (default: 0.95)
**What it controls:** Success rate percentage needed before increasing concurrency.

**Behavior:** System increases concurrency only if success rate > this threshold.

**When to adjust:**
- **Increase to 0.98** for very conservative scaling (rarely increases concurrency)
- **Decrease to 0.90** for aggressive scaling (accepts more failures)

**Recommended values:**
- Production: 0.95-0.97
- Testing: 0.90
- Unstable API: 0.98

---

### AdaptiveThrottleWindowSize (default: 00:01:00)
**What it controls:** Time window for calculating success rates.

**Behavior:** System looks at last N minutes of results to decide scaling.

**When to adjust:**
- **Increase to 00:02:00** for more stable, slower adaptation
- **Decrease to 00:00:30** for faster response to rate limits

**Recommended values:**
- Fast-changing conditions: 30 seconds
- Standard: 1 minute
- Stable conditions: 2-3 minutes

---

## Tuning Scenarios

### Scenario 1: Getting Rate Limited Frequently
**Symptoms:** Logs show many "Rate limit error: true" messages, frequent concurrency decreases

**Solutions:**
1. Decrease `SalaryExtractionMaxConcurrency` to 50-70% of current value
2. Decrease `SalaryExtractionInitialConcurrency` to 3
3. Increase `AdaptiveThrottleSuccessThreshold` to 0.97

**Example:**
```json
"SalaryExtractionInitialConcurrency": 3,
"SalaryExtractionMaxConcurrency": 10,
"AdaptiveThrottleSuccessThreshold": 0.97
```

---

### Scenario 2: Too Slow, Not Using Full Capacity
**Symptoms:** Concurrency stays low, no rate limit errors, slow throughput

**Solutions:**
1. Increase `SalaryExtractionInitialConcurrency` to 8-10
2. Increase `SalaryExtractionMaxConcurrency` to 30
3. Decrease `AdaptiveThrottleSuccessThreshold` to 0.92

**Example:**
```json
"SalaryExtractionInitialConcurrency": 10,
"SalaryExtractionMaxConcurrency": 30,
"AdaptiveThrottleSuccessThreshold": 0.92
```

---

### Scenario 3: Memory Pressure / Out of Memory
**Symptoms:** Application crashes or slows down, high memory usage

**Solutions:**
1. Decrease `SalaryExtractionChunkSize` to 500
2. Decrease `SalaryExtractionMaxConcurrency` to reduce parallel load
3. Check database connection pool isn't leaking

**Example:**
```json
"SalaryExtractionChunkSize": 500,
"SalaryExtractionMaxConcurrency": 10
```

---

### Scenario 4: Unstable Concurrency (Constantly Adjusting)
**Symptoms:** Logs show frequent increases/decreases in concurrency

**Solutions:**
1. Increase `AdaptiveThrottleWindowSize` to "00:02:00"
2. Narrow the range: increase initial, decrease max
3. Increase success threshold slightly

**Example:**
```json
"SalaryExtractionInitialConcurrency": 8,
"SalaryExtractionMaxConcurrency": 12,
"AdaptiveThrottleSuccessThreshold": 0.96,
"AdaptiveThrottleWindowSize": "00:02:00"
```

---

## Monitoring & Metrics

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
Throughput (ads/sec) ? AvgConcurrency / AvgApiResponseTime

Example:
- 10 concurrent calls
- 0.5 second avg API response
= 10 / 0.5 = 20 ads/second
= 1,200 ads/minute
```

**Estimated completion time:**
```
Time = TotalAds / (AvgConcurrency / AvgApiResponseTime)

Example:
- 10,000 ads
- 8 concurrent calls
- 0.4 second avg response
= 10,000 / (8 / 0.4) = 500 seconds ? 8 minutes
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

## Troubleshooting Checklist

- [ ] Check API rate limits in provider documentation
- [ ] Verify database connection pool size ? max concurrency
- [ ] Monitor memory usage during execution
- [ ] Review logs for error patterns
- [ ] Test with small dataset before full run
- [ ] Adjust one parameter at a time
- [ ] Document changes and their effects

---

## Advanced Tips

1. **Start Conservative**: Begin with low values and increase gradually
2. **One Change at a Time**: Only adjust one parameter per test run
3. **Log Analysis**: Grep logs for "Adaptive limiter" to see behavior patterns
4. **A/B Testing**: Try different configs on same dataset to compare
5. **Peak Hours**: API performance may vary by time of day
6. **Burst vs Sustained**: Short bursts can handle higher concurrency than sustained load
