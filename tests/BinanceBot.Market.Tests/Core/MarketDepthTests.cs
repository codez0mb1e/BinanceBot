using Binance.Net.Objects.Models;
using BinanceBot.Market.Domain;
using ContractType = BinanceBot.Market.Domain.ContractType;

namespace BinanceBot.Market.Tests.Core;

public class MarketDepthTests
{
    private static MarketDepth CreateTestMarketDepth() => 
        new MarketDepth(new MarketSymbol("BTC", "USDT", ContractType.Spot));
    
    private static MarketDepth CreateTestMarketDepthPerpetual() => 
        new MarketDepth(new MarketSymbol("BTC", "USDT", ContractType.Futures));
   
    [Theory]
    [InlineData(ContractType.Spot)]
    [InlineData(ContractType.Futures)]
    public void Constructor_WithValidSymbol_CreatesInstance(ContractType contractType)
    {
        // Arrange
        var symbol = new MarketSymbol("BTC", "USDT", contractType);
        
        // Act
        var marketDepth = new MarketDepth(symbol);

        // Assert
        Assert.Equal(symbol, marketDepth.Symbol);
        Assert.Equal(contractType, marketDepth.Symbol.ContractType);
        Assert.Null(marketDepth.LastUpdateId);
        Assert.Empty(marketDepth.Asks);
        Assert.Empty(marketDepth.Bids);
    }

    [Fact]
    public void Constructor_WithNullSymbol_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MarketDepth(null));
    }

    [Fact]
    public void UpdateDepth_WithValidData_UpdatesOrderBook()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m },
            new() { Price = 50100m, Quantity = 2.0m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m },
            new() { Price = 49800m, Quantity = 0.5m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        Assert.Equal(123456, marketDepth.LastUpdateId);
        Assert.Equal(2, marketDepth.Asks.Count());
        Assert.Equal(2, marketDepth.Bids.Count());
        Assert.Equal(50000m, marketDepth.BestAsk.Price);
        Assert.Equal(49900m, marketDepth.BestBid.Price);
    }

    [Fact]
    public void UpdateDepth_WithValidData_UpdatesOrderBook_Perpetual()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepthPerpetual();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m },
            new() { Price = 50100m, Quantity = 2.0m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m },
            new() { Price = 49800m, Quantity = 0.5m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        Assert.Equal(ContractType.Futures, marketDepth.Symbol.ContractType);
        Assert.Equal(123456, marketDepth.LastUpdateId);
        Assert.Equal(2, marketDepth.Asks.Count());
        Assert.Equal(2, marketDepth.Bids.Count());
        Assert.Equal(50000m, marketDepth.BestAsk.Price);
        Assert.Equal(49900m, marketDepth.BestBid.Price);
    }

    [Fact]
    public void UpdateDepth_WithOldUpdateTime_IgnoresUpdate()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Act - try to update with older timestamp
        var newAsks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 51000m, Quantity = 2.0m }
        };
        marketDepth.UpdateDepth(newAsks, bids, 123400);

        // Assert - should still have old data
        Assert.Equal(123456, marketDepth.LastUpdateId);
        Assert.Equal(50000m, marketDepth.BestAsk.Price);
    }

    [Fact]
    public void UpdateDepth_RemovesPriceLevelWithZeroQuantity()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m },
            new() { Price = 50100m, Quantity = 2.0m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Act - update with zero quantity to remove price level
        var updateAsks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 0m }
        };
        marketDepth.UpdateDepth(updateAsks, bids, 123457);

        // Assert
        Assert.Single(marketDepth.Asks);
        Assert.Equal(50100m, marketDepth.BestAsk.Price);
    }

    [Fact]
    public void BestPair_WhenOrderBookIsEmpty_ReturnsNull()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        Assert.Null(marketDepth.BestPair);
    }

    [Fact]
    public void BestPair_WhenOrderBookHasData_ReturnsPair()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Act
        var bestPair = marketDepth.BestPair;

        // Assert
        Assert.NotNull(bestPair);
        Assert.Equal(50000m, bestPair.Ask.Price);
        Assert.Equal(49900m, bestPair.Bid.Price);
        Assert.Equal(100m, bestPair.PriceSpread);
    }

    [Fact]
    public void BestPair_WhenOrderBookHasData_ReturnsPair_Perpetual()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepthPerpetual();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Act
        var bestPair = marketDepth.BestPair;

        // Assert
        Assert.NotNull(bestPair);
        Assert.Equal(50000m, bestPair.Ask.Price);
        Assert.Equal(49900m, bestPair.Bid.Price);
        Assert.Equal(100m, bestPair.PriceSpread);
        Assert.Equal(ContractType.Futures, marketDepth.Symbol.ContractType);
    }

    [Fact]
    public void MarketDepthChanged_RaisesEvent_WhenDepthUpdated()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        MarketDepthChangedEventArgs? eventArgs = null;
        marketDepth.MarketDepthChanged += (sender, e) => eventArgs = e;

        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(123456, eventArgs.UpdateTime);
        Assert.Single(eventArgs.Asks);
    }

    [Fact]
    public void MarketBestPairChanged_RaisesEvent_WhenBestPairChanges()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var eventRaised = false;
        marketDepth.MarketBestPairChanged += (sender, e) => eventRaised = true;

        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Asks_AreSortedAscending()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50100m, Quantity = 2.0m },
            new() { Price = 50000m, Quantity = 1.5m },
            new() { Price = 50200m, Quantity = 1.0m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49900m, Quantity = 1.0m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        var askPrices = marketDepth.Asks.Select(a => a.Price).ToList();
        Assert.Equal(new[] { 50000m, 50100m, 50200m }, askPrices);
    }

    [Fact]
    public void Bids_AreSortedDescending()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.0m }
        };
        var bids = new List<BinanceOrderBookEntry>
        {
            new() { Price = 49800m, Quantity = 0.5m },
            new() { Price = 49900m, Quantity = 1.0m },
            new() { Price = 49700m, Quantity = 2.0m }
        };

        // Act
        marketDepth.UpdateDepth(asks, bids, 123456);

        // Assert
        var bidPrices = marketDepth.Bids.Select(b => b.Price).ToList();
        Assert.Equal(new[] { 49900m, 49800m, 49700m }, bidPrices);
    }

    [Fact]
    public void UpdateDepth_WithZeroOrNegativeUpdateTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => marketDepth.UpdateDepth(asks, null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => marketDepth.UpdateDepth(asks, null, -1));
    }
}
