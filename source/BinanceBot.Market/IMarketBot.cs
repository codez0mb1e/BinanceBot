using System.Collections.Generic;
using System.Threading.Tasks;

using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;


namespace BinanceBot.Market
{
    /// <summary>
    /// Market Bot Interface
    /// </summary>
    public interface IMarketBot
    {
        /// <summary>
        /// Symbol
        /// </summary>
        string Symbol { get; }


        /// <summary>
        /// Run bot
        /// </summary>
        Task RunAsync();

        /// <summary>
        /// Stop bot
        /// </summary>
        void Stop();


        /// <summary>
        /// Validate connection w/ stock
        /// </summary>
        Task ValidateConnectionAsync();

        /// <summary>
        /// Get currently opened orders
        /// </summary>
        /// <param name="symbol"></param>
        Task<IEnumerable<OrderResponse>> GetOpenedOrdersAsync(string symbol);

        /// <summary>
        /// Create new order
        /// </summary>
        /// <param name="order"></param>
        Task<BaseCreateOrderResponse> CreateOrderAsync(CreateOrderRequest order);

        /// <summary>
        /// Cancel orders
        /// </summary>
        /// <param name="orders"></param>
        Task CancelOrdersAsync(IEnumerable<OrderResponse> orders);
    }
}