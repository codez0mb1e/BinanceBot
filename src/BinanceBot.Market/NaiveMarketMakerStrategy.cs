using System;
using Binance.Net.Enums;
using NLog;


namespace BinanceBot.Market
{
    /// <summary>
    /// Market Maker strategy (naive version)
    /// </summary>
    public class NaiveMarketMakerStrategy : IMarketStrategy
    {
        private readonly MarketStrategyConfiguration _marketStrategyConfig;
        private readonly Logger _logger;


        public NaiveMarketMakerStrategy(MarketStrategyConfiguration marketStrategyConfig, Logger logger)
        {
            _marketStrategyConfig = marketStrategyConfig ?? throw new ArgumentNullException(nameof(marketStrategyConfig));
            _logger = logger ?? LogManager.GetCurrentClassLogger();
        }


        public MarketStrategyConfiguration Config => _marketStrategyConfig;


        /// <summary>
        /// Process new best <see cref="MarketDepthPair"/> 
        /// </summary>
        /// <param name="marketPair">Best ask-bid pair</param>
        /// <returns>Recommended price-volume pair or <see langword="null"/></returns>
        public Quote Process(MarketDepthPair marketPair)
        {
            if (marketPair == null)
                throw new ArgumentNullException(nameof(marketPair));
            if (!marketPair.IsFullPair)
                return null;


            Quote quote = null;


            _logger.Info($"Best ask / bid: {marketPair.Ask.Price} / {marketPair.Bid.Price}. Update Id: {marketPair.UpdateTime}.");

            // get price spreads (in percent)
            decimal actualSpread = marketPair.PriceSpread!.Value / marketPair.MediumPrice!.Value * 100; // spread_relative = spread_absolute/price * 100
            decimal expectedSpread = _marketStrategyConfig.TradeWhenSpreadGreaterThan;

            _logger.Info($"Spread absolute / relative: {marketPair.PriceSpread} / {actualSpread:F3}%. Update Id: {marketPair.UpdateTime}.");


            if (actualSpread >= expectedSpread)
            {
                // compute new order price
                decimal extra = marketPair.MediumPrice.Value * (actualSpread - expectedSpread) / 100; // extra = medium_price * (spread_actual - spread_expected)
                decimal orderPrice = marketPair.Bid.Price + extra; // new_price = best_bid + extra

                // compute order volume
                decimal volumeSpread = marketPair.VolumeSpread!.Value;
                decimal orderVolume = volumeSpread > _marketStrategyConfig.MaxOrderVolume ? 
                    _marketStrategyConfig.MaxOrderVolume : // set max volume
                    (volumeSpread < _marketStrategyConfig.MinOrderVolume ? _marketStrategyConfig.MinOrderVolume : volumeSpread); // set min volume

                // return new price-volume pair
                quote = new Quote(orderPrice, orderVolume, OrderSide.Buy); 
            }


            return quote;
        }
    }
}