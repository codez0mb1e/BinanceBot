using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;

namespace BinanceExchange.API.Client
{
    public interface IBinanceRestClient
    {
        /// <summary>
        ///     Test the connectivity to the API
        /// </summary>
        Task<EmptyResponse> TestConnectivityAsync();

        /// <summary>
        ///     Get the current server time (UTC)
        /// </summary>
        /// <returns>
        ///     <see cref="ServerTimeResponse" />
        /// </returns>
        Task<ServerTimeResponse> GetServerTimeAsync();

        /// <summary>
        ///     Gets the current order book for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbole to retrieve the order book for</param>
        /// <param name="useCache"></param>
        /// <param name="limit">Amount to request - defaults to 100</param>
        /// <returns></returns>
        Task<OrderBookResponse> GetOrderBookAsync(string symbol, bool useCache = false, int limit = 100);


        /// <summary>
        ///     Creates an order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CreateOrderRequest" /> that is used to define the order</param>
        /// <returns></returns>
        Task<BaseCreateOrderResponse> CreateOrderAsync(CreateOrderRequest request);

        /// <summary>
        ///     Creates an test order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="EmptyResponse" /></param>
        /// <returns></returns>
        Task<EmptyResponse> CreateTestOrderAsync(CreateOrderRequest request);


        /// <summary>
        ///     Cancels an order based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CancelOrderRequest" /> that is used to define the order</param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        Task<CancelOrderResponse> CancelOrderAsync(CancelOrderRequest request, int receiveWindow = 5000);

        /// <summary>
        ///     Queries all orders based on the provided request
        /// </summary>
        /// <param name="request">The <see cref="CurrentOpenOrdersRequest" /> that is used to define the orders</param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        Task<List<OrderResponse>> GetCurrentOpenOrdersAsync(CurrentOpenOrdersRequest request, int receiveWindow = 5000);
    }
}