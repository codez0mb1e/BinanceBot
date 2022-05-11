using System;
using System.Threading.Tasks;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using BinanceBot.Market.Core;
using CryptoExchange.Net.Objects;


namespace BinanceBot.Market
{
    /// <summary>
    /// <see cref="MarketDepth"/> manager
    /// </summary>
    /// <remarks>
    /// How to manage a local order book correctly [1]: 
    ///    1. Open a stream to wss://stream.binance.com:9443/ws/bnbbtc@depth
    ///    2. Buffer the events you receive from the stream
    /// -> 3. Get a depth snapshot from https://www.binance.com/api/v1/depth?symbol=BNBBTC&amp;limit=1000
    ///    4. Drop any event where u is less or equal lastUpdateId in the snapshot
    ///    5. The first processed should have U less or equal lastUpdateId+1 AND u equal or greater lastUpdateId+1
    ///    6. While listening to the stream, each new event's U should be equal to the previous event's u+1
    ///    7. The data in each event is the absolute quantity for a price level
    ///    8. If the quantity is 0, remove the price level
    ///    9. Receiving an event that removes a price level that is not in your local order book can happen and is normal.
    /// Reference:
    ///     1. https://github.com/binance-exchange/binance-official-api-docs/blob/master/web-socket-streams.md#how-to-manage-a-local-order-book-correctly
    /// </remarks>
    public class MarketDepthManager
    {
        private readonly IBinanceClient _restClient;
        private readonly IBinanceSocketClient _webSocketClient;


        /// <summary>
        /// Create instance of <see cref="MarketDepthManager"/>
        /// </summary>
        /// <param name="binanceRestClient">Binance REST client</param>
        /// <param name="webSocketClient">Binance WebSocket client</param>
        /// <exception cref="ArgumentNullException"><paramref name="binanceRestClient"/> cannot be <see langword="null"/></exception>
        /// <exception cref="ArgumentNullException"><paramref name="webSocketClient"/> cannot be <see langword="null"/></exception>
        public MarketDepthManager(IBinanceClient binanceRestClient, IBinanceSocketClient webSocketClient)
        {
            _restClient = binanceRestClient ?? throw new ArgumentNullException(nameof(binanceRestClient));
            _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        }


        /// <summary>
        /// Build <see cref="MarketDepth"/>
        /// </summary>
        /// <param name="marketDepth">Market depth</param>
        /// <param name="limit">Limit of returned orders count</param>
        public async Task BuildAsync(MarketDepth marketDepth, short limit = 10)
        {
            if (marketDepth == null)
                throw new ArgumentNullException(nameof(marketDepth));
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            WebCallResult<BinanceOrderBook> response = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(marketDepth.Symbol, limit);
            BinanceOrderBook orderBook = response.Data;

            marketDepth.UpdateDepth(orderBook.Asks, orderBook.Bids, orderBook.LastUpdateId);
        }


        /// <summary>
        /// Stream <see cref="MarketDepth"/> updates
        /// </summary>
        /// <param name="marketDepth">Market depth</param>
        /// <param name="updateInterval"></param>
        public void StreamUpdates(MarketDepth marketDepth, TimeSpan? updateInterval = default)
        {
            if (marketDepth == null)
                throw new ArgumentNullException(nameof(marketDepth));

            _webSocketClient.SpotStreams.SubscribeToOrderBookUpdatesAsync(
                marketDepth.Symbol,
                (int)updateInterval?.TotalMilliseconds, 
                marketData => marketDepth.UpdateDepth(marketData.Data.Asks, marketData.Data.Bids, marketData.Data.LastUpdateId));
        }
    }
}