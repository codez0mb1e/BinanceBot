using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Futures;
using BinanceBot.Market.Domain;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using NLog;


namespace BinanceBot.Market;

/// <summary>
/// <see cref="MarketDepth"/> Manager.
///
/// Manages local order book synchronization with Binance WebSocket streams following official guidelines.
/// Implements the 7-step algorithm: (1) Open WebSocket, (2) Buffer events, (3) Get snapshot, 
/// (4) Validate snapshot, (5) Discard old events, (6) Apply snapshot, (7) Apply buffered and live updates.
/// 
/// See full instructions at https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md#how-to-manage-a-local-order-book-correctly
/// </summary>
public class MarketDepthManager
{    
    private readonly IBinanceClient _restClient;
    private readonly IBinanceSocketClient _webSocketClient;
    private readonly Logger _logger;
    
    private readonly Queue<IBinanceEventOrderBook> _eventBuffer = new();
    private long _localOrderBookUpdateId = 0;
    private bool _isSnapshotLoaded = false;

    private readonly TimeSpan _defaultUpdateInterval = TimeSpan.FromMilliseconds(100);
    
    private UpdateSubscription _subscription;


    /// <summary>
    /// Create instance of <see cref="MarketDepthManager"/>
    /// </summary>
    /// <param name="restClient">Binance REST client</param>
    /// <param name="webSocketClient">Binance WebSocket client</param>
    /// <param name="logger">Logger instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="restClient"/> cannot be <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="webSocketClient"/> cannot be <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be <see langword="null"/></exception>
    public MarketDepthManager(IBinanceClient restClient, IBinanceSocketClient webSocketClient, Logger logger)
    {
        _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Build <see cref="MarketDepth"/> following Binance official guidelines
    /// </summary>
    /// <param name="marketDepth">Market depth</param>
    /// <param name="orderBookDepth">Limit of returned orders count (default 10)</param>
    /// <param name="updateInterval">Update speed limit (100ms, 1000ms)</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentNullException"><paramref name="marketDepth"/> cannot be <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="updateInterval"/> must be greater than zero</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="orderBookDepth"/> must be greater than zero</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <exception cref="InvalidOperationException">Failed to subscribe to order book updates or get order book snapshot</exception>
    public async Task BuildAsync(MarketDepth marketDepth, short orderBookDepth = 10, TimeSpan? updateInterval = default, CancellationToken ct = default)
    {
        if (marketDepth == null)
            throw new ArgumentNullException(nameof(marketDepth));
        if (updateInterval.HasValue && updateInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(updateInterval));
        if (orderBookDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(orderBookDepth));

        // Step 1: Open WebSocket stream and start buffering
        _logger.Debug($"1: Opening WebSocket stream for {marketDepth.Symbol}");

        var updateIntervalMs = updateInterval.HasValue ? (int)updateInterval.Value.TotalMilliseconds : (int)_defaultUpdateInterval.TotalMilliseconds;

        CallResult<UpdateSubscription> subscriptionResult;
        
        switch (marketDepth.Symbol.ContractType)
        {
            case ContractType.Spot:
                subscriptionResult = await _webSocketClient.SpotStreams.SubscribeToOrderBookUpdatesAsync(
                    marketDepth.Symbol.FullName, updateIntervalMs,
                    data => OnSpotOrderBookUpdate(marketDepth, data),
                    ct)
                .ConfigureAwait(false);
                break;
            case ContractType.Futures:
                subscriptionResult = await _webSocketClient.UsdFuturesStreams.SubscribeToOrderBookUpdatesAsync(
                    marketDepth.Symbol.FullName, updateIntervalMs,
                    data => OnFuturesOrderBookUpdate(marketDepth, data),
                    ct)
                .ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(marketDepth.Symbol.ContractType), "Unknown contract type.");
        }
        
        if (!subscriptionResult.Success || subscriptionResult.Data == null)
            throw new InvalidOperationException($"Failed to subscribe to order book updates: {subscriptionResult.Error?.Message}");
        
        _subscription = subscriptionResult.Data;

        // Step 2: Wait a bit to buffer some events
        // Use longer buffer time to ensure we have enough events before snapshot
        var bufferTimeMs = Math.Max(updateIntervalMs * 5, 500);
        _logger.Debug($"2: Buffering events for {bufferTimeMs}ms");
        await Task.Delay(bufferTimeMs, ct).ConfigureAwait(false);

        // Step 3: Get depth snapshot
        _logger.Debug($"3: Getting order book snapshot for {marketDepth.Symbol}");
        
        (bool Success, IBinanceOrderBook Data, Error Error) response;

        switch (marketDepth.Symbol.ContractType)
        {
            case ContractType.Spot:
                WebCallResult<BinanceOrderBook> spotResponse = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(
                    marketDepth.Symbol.FullName, orderBookDepth, ct)
                .ConfigureAwait(false);

                response = (spotResponse.Success, spotResponse.Data, spotResponse.Error);
                break;
            case ContractType.Futures:
                WebCallResult<BinanceFuturesOrderBook> futuresResponse = await _restClient.UsdFuturesApi.ExchangeData.GetOrderBookAsync(
                    marketDepth.Symbol.FullName, orderBookDepth, ct)
                .ConfigureAwait(false);

                response = (futuresResponse.Success, futuresResponse.Data, futuresResponse.Error);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(marketDepth.Symbol.ContractType), "Unknown contract type.");
        }

        if (!response.Success || response.Data == null)
            throw new InvalidOperationException($"Failed to get order book snapshot: {response.Error?.Message}");

        IBinanceOrderBook snapshot = response.Data;
        _logger.Debug($"Snapshot received: LastUpdateId={snapshot.LastUpdateId}");
        
        // Step 4: Check if snapshot is valid
        // If buffered events exist and snapshot's lastUpdateId is strictly less than first event's U, retry
        IBinanceEventOrderBook firstEvent = null;
        lock (_eventBuffer)
        {
            firstEvent = _eventBuffer.Any() ? _eventBuffer.Peek() : null;
        }

        if (firstEvent != null)
        {
            _logger.Debug($"4: Validating snapshot. FirstEvent.U={firstEvent.FirstUpdateId}, Snapshot.LastUpdateId={snapshot.LastUpdateId}");
        }

        while (firstEvent != null && snapshot.LastUpdateId < firstEvent.FirstUpdateId)
        {
            _logger.Warn($"Snapshot too old: LastUpdateId={snapshot.LastUpdateId} < FirstEvent.U={firstEvent.FirstUpdateId}. Retrying...");
            // Snapshot is too old, need to get a new one
            response = await GetOrderBookSnapshotAsync(marketDepth.Symbol, orderBookDepth, ct).ConfigureAwait(false);

            if (!response.Success || response.Data == null)
                throw new InvalidOperationException($"Failed to get order book snapshot: {response.Error?.Message}");
            snapshot = response.Data;
            _logger.Debug($"New snapshot received: LastUpdateId={snapshot.LastUpdateId}");
            
            lock (_eventBuffer)
            {
                firstEvent = _eventBuffer.Any() ? _eventBuffer.Peek() : null;
            }
        }

        lock (_eventBuffer)
        {
            // Step 5: Discard buffered events where u <= lastUpdateId
            int discardedCount = 0;
            while (_eventBuffer.Any() && _eventBuffer.Peek().LastUpdateId <= snapshot.LastUpdateId)
            {
                _eventBuffer.Dequeue();
                discardedCount++;
            }
            _logger.Debug($"5: Discarded {discardedCount} outdated events (u <= {snapshot.LastUpdateId})");

            // Step 6: Set local order book to snapshot
            _logger.Debug($"6: Applying snapshot with {snapshot.Asks.Count()} asks and {snapshot.Bids.Count()} bids");
            marketDepth.UpdateDepth(snapshot.Asks, snapshot.Bids, snapshot.LastUpdateId);
            _localOrderBookUpdateId = marketDepth.LastUpdateId ?? throw new InvalidOperationException("MarketDepth.LastUpdateId is null after applying snapshot");
            _isSnapshotLoaded = marketDepth.LastUpdateId == snapshot.LastUpdateId;

            // Step 7: Apply buffered updates
            int appliedCount = 0;
            while (_eventBuffer.Any())
            {
                var bufferedEvent = _eventBuffer.Peek();
                if (bufferedEvent != null)
                {
                    ApplyDepthUpdate(marketDepth, bufferedEvent);
                    appliedCount++;
                }
                _eventBuffer.Dequeue();
            }
            _logger.Debug($"7: Applied {appliedCount} buffered events");
        }
    }


    /// <summary>
    /// Stream <see cref="MarketDepth"/> updates asynchronously.
    /// </summary>
    /// <param name="marketDepth">Market depth</param>
    /// <param name="updateInterval">Update interval (100ms or 1000ms)</param>
    /// <param name="ct">Cancellation token</param>
    public async Task StreamUpdatesAsync(MarketDepth marketDepth, TimeSpan? updateInterval = default, CancellationToken ct = default)
    {
        if (marketDepth == null)
            throw new ArgumentNullException(nameof(marketDepth));

        // Step 1 & 2: Open WebSocket and buffer events
        _logger.Debug($"1 & 2: Streaming updates: Opening WebSocket stream for {marketDepth.Symbol}");
        CallResult<UpdateSubscription> subscriptionResult;

        switch (marketDepth.Symbol.ContractType)
        {
            case ContractType.Spot:
                subscriptionResult = await _webSocketClient.SpotStreams.SubscribeToOrderBookUpdatesAsync(
                marketDepth.Symbol.FullName,
                updateInterval.HasValue ? (int)updateInterval.Value.TotalMilliseconds : (int)_defaultUpdateInterval.TotalMilliseconds,
                data => OnSpotOrderBookUpdate(marketDepth, data),
                ct)
                .ConfigureAwait(false);
                break;
            case ContractType.Futures:
                subscriptionResult = await _webSocketClient.UsdFuturesStreams.SubscribeToOrderBookUpdatesAsync(
                marketDepth.Symbol.FullName,
                updateInterval.HasValue ? (int)updateInterval.Value.TotalMilliseconds : (int)_defaultUpdateInterval.TotalMilliseconds,
                data => OnFuturesOrderBookUpdate(marketDepth, data),
                ct)
                .ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(marketDepth.Symbol.ContractType), "Unknown contract type.");
        }

        _subscription = subscriptionResult.Data;
    }
    
    /// <summary>
    /// Stop streaming updates and unsubscribe
    /// </summary>
    public async Task StopStreamingAsync(CancellationToken ct = default)
    {
        if (_subscription != null)
        {
            await _subscription.CloseAsync();
            _subscription = null;
        }
    }

    private void OnSpotOrderBookUpdate(MarketDepth marketDepth, DataEvent<IBinanceEventOrderBook> eventData) => 
        OnDepthUpdate(marketDepth, eventData.Data);

    private void OnFuturesOrderBookUpdate(MarketDepth marketDepth, DataEvent<IBinanceFuturesEventOrderBook> eventData) => 
        OnDepthUpdate(marketDepth, eventData.Data);

    private void OnDepthUpdate(MarketDepth marketDepth, IBinanceEventOrderBook eventData)
    {
        if (eventData == null) return;
        
        lock (_eventBuffer)
        {
            if (!_isSnapshotLoaded)
            {
                // Step 2: Buffer events before snapshot is loaded
                _eventBuffer.Enqueue(eventData);
                _logger.Debug($"Step 2: Buffered event U={eventData.FirstUpdateId}, u={eventData.LastUpdateId}. Buffer size: {_eventBuffer.Count}");
                return;
            }

            // Apply update to local order book
            ApplyDepthUpdate(marketDepth, eventData);
        }
    }

    private void ApplyDepthUpdate(MarketDepth marketDepth, IBinanceEventOrderBook eventData)
    {
        // Step 7: Apply update procedure
        long lastUpdateId = eventData.LastUpdateId;

        // 1. Decide whether the update event can be applied
        if (lastUpdateId <= _localOrderBookUpdateId)
        {
            // Event is older than local order book, ignore
            _logger.Debug($"Ignoring old event: u={lastUpdateId} <= local={_localOrderBookUpdateId}");
            return;
        }
        // Critical validation: check for missed updates
        if (eventData.FirstUpdateId > _localOrderBookUpdateId + 1)
        {
            _logger.Error($"Missed updates! Expected U <= {_localOrderBookUpdateId + 1}, got U={eventData.FirstUpdateId}");
            throw new InvalidOperationException(
                $"Missed order book updates. Expected U <= {_localOrderBookUpdateId + 1}, got {eventData.FirstUpdateId}. " +
                "Local order book is out of sync. Please restart the process.");
        }

        // 2. Update price levels
        if (_localOrderBookUpdateId % 100 == 0) // Log every 100th update to avoid flooding
            _logger.Debug($"Applying update: U={eventData.FirstUpdateId}, u={lastUpdateId}, Asks={eventData.Asks.Count()}, Bids={eventData.Bids.Count()}");
        
        marketDepth.UpdateDepth(eventData.Asks, eventData.Bids, lastUpdateId);
        
        // 3. Set order book update ID
        _localOrderBookUpdateId = lastUpdateId;
    }
}
