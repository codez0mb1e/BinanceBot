using System;

namespace BinanceBot.Market
{
    /// <summary>
    /// Order book's ask-bid pair
    /// </summary>
    public class MarketDepthPair
    {
        public MarketDepthPair(Quote ask, Quote bid, long updateTime)
        {
            if (updateTime <= 0) throw new ArgumentOutOfRangeException(nameof(updateTime));

            Ask = ask;
            Bid = bid;
            UpdateTime = updateTime;
        }


        public Quote Ask { get; }

        public Quote Bid { get; }

        public long UpdateTime { get; }


        public bool IsFullPair => Ask != null && Bid != null;

        public decimal? PriceSpread => IsFullPair ? Ask.Price - Bid.Price : default(decimal?);

        public decimal? VolumeSpread => IsFullPair ? Math.Abs(Ask.Volume - Bid.Volume) : default(decimal?);

        public decimal? MediumPrice => IsFullPair ? (Ask.Price + Bid.Price) / 2 : default(decimal?);
    }
}