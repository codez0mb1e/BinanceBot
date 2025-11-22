# `MarketDepthManager` Improvements

## 📋 Recommended Additional Tests

### Integration-Style Tests (High Priority)

```csharp
public class MarketDepthManagerIntegrationTests
{
    [Test]
    public async Task BuildAsync_WithRealTimeSimulation_SynchronizesCorrectly()
    {
        // Test that simulates realistic WebSocket + REST timing
        // Validates the 7-step algorithm with controlled event sequences
    }

    [Test]
    public async Task BuildAsync_WithSlowSnapshot_HandlesBufferedEvents()
    {
        // Simulates slow snapshot retrieval while events keep arriving
        // Ensures buffer doesn't overflow and events are properly queued
    }

    [Test]
    public async Task BuildAsync_WithOutdatedSnapshot_RetriesUntilValid()
    {
        // Tests snapshot validation and retry logic (Step 4)
        // Ensures system gets a fresh enough snapshot
    }

    [Theory]
    [InlineData(ContractType.Spot)]
    [InlineData(ContractType.Futures)]
    public async Task ProcessDepthUpdate_WithSequentialEvents_MaintainsOrderBook(ContractType contractType)
    {
        // Validates that sequential updates are processed correctly
        // Tests both Spot and Futures contract types
    }
}
```

### Edge Case Tests (Medium Priority)

```csharp
[Test]
public async Task BuildAsync_WithNoBufferedEvents_StillAppliesSnapshot()
{
    // Edge case: snapshot arrives before any WebSocket events
}

[Test]
public async Task ProcessDepthUpdate_WithDuplicateUpdateId_IgnoresCorrectly()
{
    // Validates idempotency - duplicate events should be ignored
}

[Test]
public async Task BuildAsync_CancellationToken_CancelsGracefully()
{
    // Tests cancellation during various stages of BuildAsync
}

[Test]
public async Task StopStreamingAsync_WithActiveSubscription_UnsubscribesCleanly()
{
    // Validates proper cleanup of WebSocket subscriptions
}
```

---

## 🔄 Suggested Future Enhancements

### 1. Configuration Object Pattern
Instead of multiple parameters, consider a configuration object:

```csharp
public class MarketDepthConfiguration
{
    public short OrderBookDepth { get; init; } = 10;
    public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan BufferTimeMultiplier { get; init; } = TimeSpan.FromMilliseconds(5);
    public int MinimumBufferTimeMs { get; init; } = 500;
}

public async Task BuildAsync(
    MarketDepth marketDepth,
    MarketDepthConfiguration config = null,
    CancellationToken ct = default)
```

### 2. Resilience Improvements
```csharp
// Add automatic reconnection on WebSocket disconnect
// Add configurable retry policy for snapshot retrieval
// Add circuit breaker pattern for repeated failures
```

### 3. Observability Enhancements
```csharp
public class MarketDepthMetrics
{
    public long TotalUpdatesProcessed { get; set; }
    public long UpdatesIgnored { get; set; }
    public long SnapshotRetries { get; set; }
    public TimeSpan AverageUpdateLatency { get; set; }
}

public MarketDepthMetrics GetMetrics() => _metrics;
```

### 4. Event-Driven Architecture
```csharp
public event EventHandler<SnapshotAppliedEventArgs> SnapshotApplied;
public event EventHandler<SynchronizationErrorEventArgs> SynchronizationError;
public event EventHandler<MetricsUpdatedEventArgs> MetricsUpdated;
```

