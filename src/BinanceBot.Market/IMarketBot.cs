using System.Collections.Generic;
using System.Threading.Tasks;
using Binance.Net.Objects.Models.Spot;


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
        Task<IEnumerable<BinanceOrder>> GetOpenedOrdersAsync(string symbol);

        /// <summary>
        /// Create new order
        /// </summary>
        /// <param name="order"></param>
        Task<BinancePlacedOrder> CreateOrderAsync(CreateOrderRequest order);

        /// <summary>
        /// Cancel orders
        /// </summary>
        /// <param name="orders"></param>
        Task CancelOrdersAsync(IEnumerable<BinanceOrder> orders);
    }
}