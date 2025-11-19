using System;
using System.Collections.Generic;
using BinanceBot.Market.Domain;

namespace BinanceBot.Market;

/// <summary>
/// Publisher of <see cref="MarketDepth"/> events
/// </summary>
public interface IMarketDepthPublisher
{
    /// <summary>
    /// Order book was changed
    /// </summary>
    event EventHandler<MarketDepthChangedEventArgs> MarketDepthChanged;

    /// <summary>
    /// Best <seealso cref="MarketDepthPair"/> was changed
    /// </summary>
    event EventHandler<MarketBestPairChangedEventArgs> MarketBestPairChanged;
}



/// <summary>
/// Order book changed event args
/// </summary>
public sealed class MarketBestPairChangedEventArgs : EventArgs
{
    public MarketBestPairChangedEventArgs(MarketDepthPair marketBestPair)
    {
        MarketBestPair = marketBestPair ?? throw new ArgumentNullException(nameof(marketBestPair));
    }

    public MarketDepthPair MarketBestPair { get; }
}


/// <summary>
/// Best <seealso cref="MarketDepthPair"/> changed event args
/// </summary>
public sealed class MarketDepthChangedEventArgs : EventArgs
{
    public MarketDepthChangedEventArgs(IEnumerable<Quote> asks, IEnumerable<Quote> bids, long updateTime)
    {
        if (updateTime <= 0) throw new ArgumentOutOfRangeException(nameof(updateTime));

        Asks = asks;
        Bids = bids;
        UpdateTime = updateTime;
    }


    public IEnumerable<Quote> Asks { get; }

    public IEnumerable<Quote> Bids { get; }

    public long UpdateTime { get; }
}