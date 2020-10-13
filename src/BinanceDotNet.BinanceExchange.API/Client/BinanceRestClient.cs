using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Utility;
using NLog;

namespace BinanceExchange.API.Client
{
    /// <summary>
    /// The Binance Client used to communicate with the official Binance API. For more information on underlying API calls
    /// </summary>
    /// <see href="https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md"/>
    public class BinanceRestClient : IBinanceRestClient
    {
        private readonly Logger _logger;
        private readonly IAPIProcessor _apiProcessor;
        private readonly int _defaultReceiveWindow;

        /// <summary>
        ///     Create a new Binance Client based on the configuration provided
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="apiProcessor"></param>
        /// <param name="logger"></param>
        public BinanceRestClient(BinanceClientConfiguration configuration, IAPIProcessor apiProcessor = null, Logger logger = null)
        {
            Guard.AgainstNull(configuration);
            Guard.AgainstNullOrEmpty(configuration.ApiKey);
            Guard.AgainstNull(configuration.SecretKey);

            _defaultReceiveWindow = configuration.DefaultReceiveWindow;
            var apiKey = configuration.ApiKey;
            var secretKey = configuration.SecretKey;
            RequestClient.SetTimestampOffset(configuration.TimestampOffset);
            RequestClient.SetRateLimiting(configuration.EnableRateLimiting);
            RequestClient.SetAPIKey(apiKey);
            if (apiProcessor == null)
            {
                _apiProcessor = new APIProcessor(apiKey, secretKey);
                _apiProcessor.SetCacheTime(configuration.CacheTime);
            }
            else
            {
                _apiProcessor = apiProcessor;
            }

            _logger = logger ?? LogManager.GetCurrentClassLogger();
        }


        #region Market Data

        /// <summary>
        ///     Gets the current depth order book for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to retrieve the order book for</param>
        /// <param name="useCache"></param>
        /// <param name="limit">Amount to request - defaults to 100</param>
        /// <returns></returns>
        public async Task<OrderBookResponse> GetOrderBookAsync(string symbol, bool useCache = false, int limit = 100)
        {
            Guard.AgainstNull(symbol);
            if (limit > 100)
                throw new ArgumentException(
                    "When requesting the order book, you can't request more than 100 at a time.", nameof(limit));
            return await _apiProcessor.ProcessGetRequest<OrderBookResponse>(
                Endpoints.MarketData.OrderBook(symbol, limit, useCache));
        }

        #endregion


        private int SetReceiveWindow(int receiveWindow)
        {
            if (receiveWindow == -1)
                receiveWindow = _defaultReceiveWindow;

            return receiveWindow;
        }


        #region General

        /// <summary>
        ///     Test the connectivity to the API
        /// </summary>
        public async Task<EmptyResponse> TestConnectivityAsync()
        {
            return await _apiProcessor.ProcessGetRequest<EmptyResponse>(Endpoints.General.TestConnectivity);
        }

        /// <summary>
        ///     Get the current server time (UTC)
        /// </summary>
        /// <returns>
        ///     <see cref="ServerTimeResponse" />
        /// </returns>
        public async Task<ServerTimeResponse> GetServerTimeAsync()
        {
            return await _apiProcessor.ProcessGetRequest<ServerTimeResponse>(Endpoints.General.ServerTime);
        }

        #endregion


        #region Account and Market

        /// <summary>
        ///     Creates an order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CreateOrderRequest" /> that is used to define the order</param>
        /// <returns>
        ///     This method can return <see cref="AcknowledgeCreateOrderResponse" />, <see cref="FullCreateOrderResponse" />
        ///     or <see cref="ResultCreateOrderResponse" /> based on the provided NewOrderResponseType enum in the request.
        /// </returns>
        public async Task<BaseCreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
        {
            Guard.AgainstNull(request.Symbol);
            Guard.AgainstNull(request.Side);
            Guard.AgainstNull(request.Type);
            Guard.AgainstNull(request.Quantity);

            switch (request.NewOrderResponseType)
            {
                case NewOrderResponseType.Acknowledge:
                    return await _apiProcessor.ProcessPostRequest<AcknowledgeCreateOrderResponse>(
                        Endpoints.Account.NewOrder(request));
                case NewOrderResponseType.Full:
                    return await _apiProcessor.ProcessPostRequest<FullCreateOrderResponse>(
                        Endpoints.Account.NewOrder(request));
                default:
                    return await _apiProcessor.ProcessPostRequest<ResultCreateOrderResponse>(
                        Endpoints.Account.NewOrder(request));
            }
        }

        /// <summary>
        ///     Creates a test order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CreateOrderRequest" /> that is used to define the order</param>
        /// <returns></returns>
        public async Task<EmptyResponse> CreateTestOrderAsync(CreateOrderRequest request)
        {
            Guard.AgainstNull(request.Symbol);
            Guard.AgainstNull(request.Side);
            Guard.AgainstNull(request.Type);
            Guard.AgainstNull(request.Quantity);

            return await _apiProcessor.ProcessPostRequest<EmptyResponse>(Endpoints.Account.NewOrderTest(request));
        }

        /// <summary>
        ///     Cancels an order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CancelOrderRequest" /> that is used to define the order</param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<CancelOrderResponse> CancelOrderAsync(CancelOrderRequest request, int receiveWindow = -1)
        {
            receiveWindow = SetReceiveWindow(receiveWindow);
            Guard.AgainstNull(request.Symbol);

            return await _apiProcessor.ProcessDeleteRequest<CancelOrderResponse>(Endpoints.Account.CancelOrder(request),
                receiveWindow);
        }

        /// <summary>
        ///     Queries all orders based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CurrentOpenOrdersRequest" /> that is used to define the orders</param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<List<OrderResponse>> GetCurrentOpenOrdersAsync(CurrentOpenOrdersRequest request,
            int receiveWindow = -1)
        {
            receiveWindow = SetReceiveWindow(receiveWindow);
            return await _apiProcessor.ProcessGetRequest<List<OrderResponse>>(
                Endpoints.Account.CurrentOpenOrders(request), receiveWindow);
        }

        #endregion
    }
}