using Binance.Net.Objects.Models;
using BinanceBot.Market.Domain;
namespace BinanceBot.Market.Tests.Core;

public class MarketDepthTests
{
    private static MarketDepth CreateTestMarketDepth() => 
        new MarketDepth(new MarketSymbol("BTC", "USDT", ContractType.Spot));
    
    private static MarketDepth CreateTestMarketDepthPerpetual() => 
        new MarketDepth(new MarketSymbol("BTC", "USDT", ContractType.Futures));
   
    [Test]
    [Arguments(ContractType.Spot)]
    [Arguments(ContractType.Futures)]
    public async Task Constructor_WithValidSymbol_CreatesInstance(ContractType contractType)
    {
        // Arrange
        var symbol = new MarketSymbol("BTC", "USDT", contractType);
        
        // Act
        var marketDepth = new MarketDepth(symbol);

        // Assert
        await Assert.That(marketDepth.Symbol).IsEqualTo(symbol);
        await Assert.That(marketDepth.Symbol.ContractType).IsEqualTo(contractType);
        await Assert.That(marketDepth.LastUpdateId).IsNull();
        await Assert.That(marketDepth.Asks).IsEmpty();
        await Assert.That(marketDepth.Bids).IsEmpty();
    }

    [Test]
    public async Task Constructor_WithNullSymbol_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketDepth(null)));
    }

    [Test]
    public async Task UpdateDepth_WithValidData_UpdatesOrderBook()
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
        await Assert.That(marketDepth.LastUpdateId).IsEqualTo(123456);
        await Assert.That(marketDepth.Asks.Count()).IsEqualTo(2);
        await Assert.That(marketDepth.Bids.Count()).IsEqualTo(2);
        await Assert.That(marketDepth.BestAsk.Price).IsEqualTo(50000m);
        await Assert.That(marketDepth.BestBid.Price).IsEqualTo(49900m);
    }

    [Test]
    public async Task UpdateDepth_WithValidData_UpdatesOrderBook_Perpetual()
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
        await Assert.That(marketDepth.Symbol.ContractType).IsEqualTo(ContractType.Futures);
        await Assert.That(marketDepth.LastUpdateId).IsEqualTo(123456);
        await Assert.That(marketDepth.Asks.Count()).IsEqualTo(2);
        await Assert.That(marketDepth.Bids.Count()).IsEqualTo(2);
        await Assert.That(marketDepth.BestAsk.Price).IsEqualTo(50000m);
        await Assert.That(marketDepth.BestBid.Price).IsEqualTo(49900m);
    }

    [Test]
    public async Task UpdateDepth_WithOldUpdateTime_IgnoresUpdate()
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
        await Assert.That(marketDepth.LastUpdateId).IsEqualTo(123456);
        await Assert.That(marketDepth.BestAsk.Price).IsEqualTo(50000m);
    }

    [Test]
    public async Task UpdateDepth_RemovesPriceLevelWithZeroQuantity()
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
        await Assert.That(marketDepth.Asks.Count()).IsEqualTo(1);
        await Assert.That(marketDepth.BestAsk.Price).IsEqualTo(50100m);
    }

    [Test]
    public async Task BestPair_WhenOrderBookIsEmpty_ReturnsNull()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        await Assert.That(marketDepth.BestPair).IsNull();
    }

    [Test]
    public async Task BestPair_WhenOrderBookHasData_ReturnsPair()
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
        await Assert.That(bestPair).IsNotNull();
        await Assert.That(bestPair!.Ask.Price).IsEqualTo(50000m);
        await Assert.That(bestPair.Bid.Price).IsEqualTo(49900m);
        await Assert.That(bestPair.PriceSpread).IsEqualTo(100m);
    }

    [Test]
    public async Task BestPair_WhenOrderBookHasData_ReturnsPair_Perpetual()
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
        await Assert.That(bestPair).IsNotNull();
        await Assert.That(bestPair!.Ask.Price).IsEqualTo(50000m);
        await Assert.That(bestPair.Bid.Price).IsEqualTo(49900m);
        await Assert.That(bestPair.PriceSpread).IsEqualTo(100m);
        await Assert.That(marketDepth.Symbol.ContractType).IsEqualTo(ContractType.Futures);
    }

    [Test]
    public async Task MarketDepthChanged_RaisesEvent_WhenDepthUpdated()
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
        await Assert.That(eventArgs).IsNotNull();
        await Assert.That(eventArgs!.UpdateTime).IsEqualTo(123456);
        await Assert.That(eventArgs.Asks.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task MarketBestPairChanged_RaisesEvent_WhenBestPairChanges()
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
        await Assert.That(eventRaised).IsTrue();
    }

    [Test]
    public async Task Asks_AreSortedAscending()
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
        await Assert.That(askPrices).IsEquivalentTo(new[] { 50000m, 50100m, 50200m });
    }

    [Test]
    public async Task Bids_AreSortedDescending()
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
        await Assert.That(bidPrices).IsEquivalentTo(new[] { 49900m, 49800m, 49700m });
    }

    [Test]
    public async Task UpdateDepth_WithZeroOrNegativeUpdateTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var marketDepth = CreateTestMarketDepth();
        var asks = new List<BinanceOrderBookEntry>
        {
            new() { Price = 50000m, Quantity = 1.5m }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            Task.Run(() => marketDepth.UpdateDepth(asks, null, 0)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            Task.Run(() => marketDepth.UpdateDepth(asks, null, -1)));
    }
}
