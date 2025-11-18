using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using BinanceBot.Market.Core;
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
    
    private UpdateSubscription _subscription;


    /// <summary>
    /// Create instance of <see cref="MarketDepthManager"/>
    /// </summary>
    /// <param name="binanceRestClient">Binance REST client</param>
    /// <param name="webSocketClient">Binance WebSocket client</param>
    /// <param name="logger">Logger instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="binanceRestClient"/> cannot be <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="webSocketClient"/> cannot be <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be <see langword="null"/></exception>
    public MarketDepthManager(IBinanceClient binanceRestClient, IBinanceSocketClient webSocketClient, Logger logger)
    {
        _restClient = binanceRestClient ?? throw new ArgumentNullException(nameof(binanceRestClient));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Build <see cref="MarketDepth"/> following Binance official guidelines
    /// </summary>
    /// <param name="marketDepth">Market depth</param>
    /// <param name="limit">Limit of returned orders count</param>
    /// <param name="updateLimit">Update speed limit (100ms, 1000ms)</param>
    public async Task BuildAsync(MarketDepth marketDepth, short limit = 10, int updateLimit = 1000)
    {
        if (marketDepth == null)
            throw new ArgumentNullException(nameof(marketDepth));
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        // Step 1: Open WebSocket stream and start buffering
        _logger.Debug($"Step 1: Opening WebSocket stream for {marketDepth.Symbol}");
        var subscriptionResult = await _webSocketClient.SpotStreams.SubscribeToOrderBookUpdatesAsync(
            marketDepth.Symbol, updateLimit,
            data => OnDepthUpdate(marketDepth, data)).ConfigureAwait(false);
        
        if (!subscriptionResult.Success || subscriptionResult.Data == null)
            throw new InvalidOperationException($"Failed to subscribe to order book updates: {subscriptionResult.Error?.Message}");
        
        _subscription = subscriptionResult.Data;

        // Step 2: Wait a bit to buffer some events
        _logger.Debug($"Step 2: Buffering events for 200ms");
        await Task.Delay(200).ConfigureAwait(false);

        _logger.Debug($"Step 3: Getting order book snapshot for {marketDepth.Symbol}");
        // Step 3: Get depth snapshot
        WebCallResult<BinanceOrderBook> response = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(marketDepth.Symbol, limit);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException($"Failed to get order book snapshot: {response.Error?.Message}");

        BinanceOrderBook snapshot = response.Data;
        _logger.Debug($"Snapshot received: LastUpdateId={snapshot.LastUpdateId}");
        
        // Step 4: Check if snapshot is valid
        // If buffered events exist and snapshot's lastUpdateId is strictly less than first event's U, retry
        BinanceEventOrderBook firstEvent = null;
        lock (_eventBuffer)
        {
            if (_eventBuffer.Count > 0)
                firstEvent = _eventBuffer.Peek() as BinanceEventOrderBook;
        }

        if (firstEvent != null)
        {
            _logger.Debug($"Step 4: Validating snapshot. FirstEvent.U={firstEvent.FirstUpdateId}, Snapshot.LastUpdateId={snapshot.LastUpdateId}");
        }

        while (firstEvent != null && snapshot.LastUpdateId < firstEvent.FirstUpdateId)
        {
            _logger.Warn($"Snapshot too old: LastUpdateId={snapshot.LastUpdateId} < FirstEvent.U={firstEvent.FirstUpdateId}. Retrying...");
            // Snapshot is too old, need to get a new one
            response = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(marketDepth.Symbol, limit);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException($"Failed to get order book snapshot: {response.Error?.Message}");
            snapshot = response.Data;
            _logger.Debug($"New snapshot received: LastUpdateId={snapshot.LastUpdateId}");
            
            lock (_eventBuffer)
            {
                if (_eventBuffer.Count > 0)
                    firstEvent = _eventBuffer.Peek() as BinanceEventOrderBook;
                else
                    firstEvent = null;
            }
        }

        lock (_eventBuffer)
        {
            // Step 5: Discard buffered events where u <= lastUpdateId
            int discardedCount = 0;
            while (_eventBuffer.Count > 0 && _eventBuffer.Peek().LastUpdateId <= snapshot.LastUpdateId)
            {
                _eventBuffer.Dequeue();
                discardedCount++;
            }
            _logger.Debug($"Step 5: Discarded {discardedCount} outdated events (u <= {snapshot.LastUpdateId})");

            // Step 6: Set local order book to snapshot
            _logger.Debug($"Step 6: Applying snapshot with {snapshot.Asks.Count()} asks and {snapshot.Bids.Count()} bids");
            marketDepth.UpdateDepth(snapshot.Asks, snapshot.Bids, snapshot.LastUpdateId);
            _localOrderBookUpdateId = snapshot.LastUpdateId;
            _isSnapshotLoaded = true;

            // Step 7: Apply buffered updates
            int appliedCount = 0;
            while (_eventBuffer.Count > 0)
            {
                var bufferedEvent = _eventBuffer.Peek() as BinanceEventOrderBook;
                if (bufferedEvent != null)
                {
                    ApplyDepthUpdate(marketDepth, bufferedEvent);
                    appliedCount++;
                }
                _eventBuffer.Dequeue();
            }
            _logger.Debug($"Step 7: Applied {appliedCount} buffered events");
        }
    }


    /// <summary>
    /// Stream <see cref="MarketDepth"/> updates
    /// </summary>
    /// <param name="marketDepth">Market depth</param>
    /// <param name="updateInterval">Update interval (100ms or 1000ms)</param>
    public void StreamUpdates(MarketDepth marketDepth, TimeSpan? updateInterval = default)
    {
        if (marketDepth == null)
            throw new ArgumentNullException(nameof(marketDepth));

        // Step 1 & 2: Open WebSocket and buffer events
        _subscription = _webSocketClient.SpotStreams.SubscribeToOrderBookUpdatesAsync(
            marketDepth.Symbol,
            updateInterval.HasValue ? (int)updateInterval.Value.TotalMilliseconds : 1000,
            data => OnDepthUpdate(marketDepth, data)).Result.Data;
    }

    /// <summary>
    /// Stop streaming updates and unsubscribe
    /// </summary>
    public async Task StopStreamingAsync()
    {
        if (_subscription != null)
        {
            await _subscription.CloseAsync();
            _subscription = null;
        }
    }

    private void OnDepthUpdate(MarketDepth marketDepth, DataEvent<IBinanceEventOrderBook> dataEvent)
    {
        var data = dataEvent.Data as BinanceEventOrderBook;
        if (data == null) return;
        
        lock (_eventBuffer)
        {
            if (!_isSnapshotLoaded)
            {
                // Step 2: Buffer events before snapshot is loaded
                _eventBuffer.Enqueue(dataEvent.Data);
                _logger.Debug($"Step 2: Buffered event U={data.FirstUpdateId}, u={data.LastUpdateId}. Buffer size: {_eventBuffer.Count}");
                return;
            }

            // Apply update to local order book
            ApplyDepthUpdate(marketDepth, data);
        }
    }

    private void ApplyDepthUpdate(MarketDepth marketDepth, BinanceEventOrderBook eventData)
    {
        // Step 7: Apply update procedure
        
        // 1. Decide whether the update event can be applied
        if (eventData.LastUpdateId <= _localOrderBookUpdateId)
        {
            // Event is older than local order book, ignore
            _logger.Debug($"Ignoring old event: u={eventData.LastUpdateId} <= local={_localOrderBookUpdateId}");
            return;
        }

        if (eventData.FirstUpdateId > _localOrderBookUpdateId + 1)
        {
            // Missed some events - need to restart
            _logger.Error($"Missed updates! Expected U <= {_localOrderBookUpdateId + 1}, got U={eventData.FirstUpdateId}");
            throw new InvalidOperationException(
                $"Missed order book updates. Expected U <= {_localOrderBookUpdateId + 1}, got {eventData.FirstUpdateId}. " +
                "Local order book is out of sync. Please restart the process.");
        }

        // Normally U of next event should equal u + 1 of previous event
        // This is handled by the check above

        // 2. Update price levels
        if (_localOrderBookUpdateId % 100 == 0) // Log every 100th update to avoid flooding
            _logger.Debug($"Applying update: U={eventData.FirstUpdateId}, u={eventData.LastUpdateId}, Asks={eventData.Asks.Count()}, Bids={eventData.Bids.Count()}");
        marketDepth.UpdateDepth(eventData.Asks, eventData.Bids, eventData.LastUpdateId);
        
        // 3. Set order book update ID
        _localOrderBookUpdateId = eventData.LastUpdateId;
    }
}