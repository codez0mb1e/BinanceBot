using System;
using System.Collections.Generic;
using System.Linq;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using BinanceBot.Market.Utility;


namespace BinanceBot.Market
{
    /// <summary>
    /// Order book
    /// </summary>
    public class MarketDepth : IMarketDepthPublisher
    {
        public MarketDepth(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Invalid symbol value", nameof(symbol));

            Symbol = symbol;
        }


        public string Symbol { get; }


        #region Ask section
        private readonly IDictionary<decimal, decimal> _asks = new SortedDictionary<decimal, decimal>();

        /// <summary>
        /// Get prices that a seller is willing to receive a symbol.
        /// Asks sorted by ascending price. The first (best) ask will be the minimum price.
        /// </summary>
        public IEnumerable<Quote> Asks => _asks.ToQuotes(OrderSide.Sell);

        /// <summary>
        /// The best ask. If the order book does not contain asks, will be returned <see langword="null"/>.
        /// </summary>
        public Quote BestAsk => Asks.FirstOrDefault();
        #endregion


        #region Bid section
        private readonly IDictionary<decimal, decimal> _bids = new SortedDictionary<decimal, decimal>(new DescendingDecimalComparer());

        /// <summary>
        /// Get prices that a buyer is willing to pay for a symbol.
        /// Bids sorted by descending price. The first (best) bid will be the maximum price.
        /// </summary>
        public IEnumerable<Quote> Bids => _bids.ToQuotes(OrderSide.Buy);

        /// <summary>
        /// The best bid. If the order book does not contain bids, will be returned <see langword="null"/>.
        /// </summary>
        public Quote BestBid => Bids.FirstOrDefault();
        #endregion


        /// <summary>
        /// The best pair. If the order book is empty, will be returned <see langword="null"/>.
        /// </summary>
        public MarketDepthPair BestPair => LastUpdateTime.HasValue 
            ? new MarketDepthPair(BestAsk, BestBid, LastUpdateTime.Value) 
            : null;

        /// <summary>
        /// Last update of market depth
        /// </summary>
        public long? LastUpdateTime { get; private set; }



        #region Update depth section
        private const decimal IgnoreVolumeValue = 0.00000000M;

        /// <summary>
        /// Update market depth
        /// </summary>
        public void UpdateDepth(IEnumerable<BinanceOrderBookEntry> asks, IEnumerable<BinanceOrderBookEntry> bids, long updateTime)
        {
            if (updateTime <= 0)
                throw new ArgumentOutOfRangeException(nameof(updateTime));

            // if nothing was changed then return
            if (updateTime <= LastUpdateTime) return;
            if (asks == null && bids == null) return;


            static void UpdateOrderBook(IEnumerable<BinanceOrderBookEntry> updates, IDictionary<decimal, decimal> orders)
            {
                if (updates == null) return;

                foreach (BinanceOrderBookEntry t in updates)
                {
                    if (t.Quantity != IgnoreVolumeValue)
                        orders[t.Price] = t.Quantity;
                    else
                    if (orders.ContainsKey(t.Price)) orders.Remove(t.Price);
                }
            }

            // save prev BestPair to OnMarketBestPairChanged raise event
            MarketDepthPair prevBestPair = BestPair;
            // update asks market depth
            UpdateOrderBook(asks, _asks);
            UpdateOrderBook(bids, _bids);
            // set new update time
            LastUpdateTime = updateTime;

            // raise events
            OnMarketDepthChanged(new MarketDepthChangedEventArgs(Asks, Bids, LastUpdateTime.Value));
            if(!BestPair.Equals(prevBestPair))
                OnMarketBestPairChanged(new MarketBestPairChangedEventArgs(BestPair));
        }
        #endregion


        #region Market Depth events
        public event EventHandler<MarketDepthChangedEventArgs> MarketDepthChanged;

        protected virtual void OnMarketDepthChanged(MarketDepthChangedEventArgs e)
        {
            EventHandler<MarketDepthChangedEventArgs> handler = MarketDepthChanged;
            handler?.Invoke(this, e);
        }


        public event EventHandler<MarketBestPairChangedEventArgs> MarketBestPairChanged;

        protected virtual void OnMarketBestPairChanged(MarketBestPairChangedEventArgs e)
        {
            EventHandler<MarketBestPairChangedEventArgs> handler = MarketBestPairChanged;
            handler?.Invoke(this, e);
        }
        #endregion
    }
}