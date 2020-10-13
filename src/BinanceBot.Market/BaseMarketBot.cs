using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using NLog;

namespace BinanceBot.Market
{
    /// <summary>
    /// Base Market Bot
    /// </summary>
    /// <typeparam name="TStrategy"></typeparam>
    public abstract class BaseMarketBot<TStrategy> :
        IMarketBot, IDisposable
        where TStrategy : class, IMarketStrategy
    {
        protected readonly Logger Logger;


        protected BaseMarketBot(string symbol, TStrategy marketStrategy, Logger logger)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            MarketStrategy = marketStrategy ?? throw new ArgumentNullException(nameof(marketStrategy));
            Logger = logger ?? LogManager.GetCurrentClassLogger();
        }


        public string Symbol { get; }

        public TStrategy MarketStrategy { get; }


        public abstract Task RunAsync();

        public abstract void Stop();

        public abstract Task ValidateConnectionAsync();

        public abstract Task<IEnumerable<OrderResponse>> GetOpenedOrdersAsync(string symbol);

        public abstract Task CancelOrdersAsync(IEnumerable<OrderResponse> orders);

        public abstract Task<BaseCreateOrderResponse> CreateOrderAsync(CreateOrderRequest order);

        
        public abstract void Dispose();
    }
}