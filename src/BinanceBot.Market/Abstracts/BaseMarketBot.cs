using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Binance.Net.Objects.Models.Spot;
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

        protected readonly TStrategy MarketStrategy;


        protected BaseMarketBot(string symbol, TStrategy marketStrategy, Logger logger)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            MarketStrategy = marketStrategy ?? throw new ArgumentNullException(nameof(marketStrategy));
            Logger = logger ?? LogManager.GetCurrentClassLogger();
        }


        public string Symbol { get; }


        public abstract Task RunAsync();

        public abstract void Stop();

        public abstract Task ValidateServerTimeAsync();

        public abstract Task<IEnumerable<BinanceOrder>> GetOpenedOrdersAsync(string symbol);

        public abstract Task CancelOrdersAsync(IEnumerable<BinanceOrder> orders);

        public abstract Task<BinancePlacedOrder> CreateOrderAsync(CreateOrderRequest order);

        
        public abstract void Dispose();
    }
}