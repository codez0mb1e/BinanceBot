using System;

namespace BinanceBot.Market.Domain;

/// <summary>
/// Order book's ask-bid pair
/// </summary>
public record MarketDepthPair
{
    public MarketDepthPair(Quote ask, Quote bid, long updateTime)
    {
        if (ask == null) 
            throw new ArgumentNullException(nameof(ask));
        if (bid == null) 
            throw new ArgumentNullException(nameof(bid));
        if (ask.Price < bid.Price)  
            throw new ArgumentException("Ask price must be greater than or equal to bid price.");
        if (updateTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(updateTime));
        
        Ask = ask;
        Bid = bid;
        UpdateTime = updateTime;
    }


    public Quote Ask { get; }

    public Quote Bid { get; }

    public long UpdateTime { get; }

    /// <summary>
    /// Flag that <see cref="MarketDepth"/> has orders on 2 sides
    /// </summary>
    public bool IsFull => Ask != null && Bid != null;

    public decimal? PriceSpread => IsFull ? Ask.Price - Bid.Price : default;

    public decimal? VolumeSpread => IsFull ? Math.Abs(Ask.Volume - Bid.Volume) : default;

    public decimal? MediumPrice => IsFull ? (Ask.Price + Bid.Price)/2 : default;
}