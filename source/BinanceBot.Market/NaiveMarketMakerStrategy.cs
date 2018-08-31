using System;
using BinanceExchange.API.Enums;
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

            // spread in percentage points: spread_in_pp = spread/price * 100
            decimal spreadInPP = marketPair.PriceSpread.Value / marketPair.MediumPrice.Value * 100;

            _logger.Info($"Best ask / bid: {marketPair.Ask.Price} / {marketPair.Bid.Price}. Update Id: {marketPair.UpdateTime}.");
            _logger.Info($"Spread absolute / relative: {marketPair.PriceSpread} / {spreadInPP:F3}%. Update Id: {marketPair.UpdateTime}.");

            if (spreadInPP >= _marketStrategyConfig.TradeWhenSpreadGreaterThan)
            {
                // compute order price
                decimal orderPrice = marketPair.Bid.Price + marketPair.MediumPrice.Value * _marketStrategyConfig.TradeWhenSpreadGreaterThan / 100;

                // compute order volume
                decimal orderVolume = marketPair.VolumeSpread.Value > _marketStrategyConfig.MaxOrderVolume
                    ? _marketStrategyConfig.MaxOrderVolume // set max volume
                    : (marketPair.VolumeSpread.Value < _marketStrategyConfig.MinOrderVolume
                        ? _marketStrategyConfig.MinOrderVolume // set min volume
                        : marketPair.VolumeSpread.Value);

                // return price-volume pair
                quote = new Quote(orderPrice, orderVolume, OrderSide.Buy); 
            }


            return quote;
        }
    }
}