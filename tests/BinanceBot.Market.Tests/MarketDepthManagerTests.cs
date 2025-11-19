using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Interfaces.Clients;
using BinanceBot.Market.Core;
using Moq;
using NLog;

namespace BinanceBot.Market.Tests;

public class MarketDepthManagerTests
{
    private readonly Mock<IBinanceClient> _mockRestClient;
    private readonly Mock<IBinanceSocketClient> _mockSocketClient;
    private readonly Mock<Logger> _mockLogger;

    public MarketDepthManagerTests()
    {
        _mockRestClient = new Mock<IBinanceClient>();
        _mockSocketClient = new Mock<IBinanceSocketClient>();
        _mockLogger = new Mock<Logger>();
    }

    [Fact]
    public void Constructor_WithNullRestClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MarketDepthManager(null, _mockSocketClient.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullSocketClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MarketDepthManager(_mockRestClient.Object, null, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, null));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task BuildAsync_WithNullMarketDepth_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            manager.BuildAsync(null));
    }

    [Fact]
    public async Task BuildAsync_WithZeroOrderBookDepth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = new MarketDepth("BTCUSDT");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: null, orderBookDepth: 0));
    }

    [Fact]
    public async Task BuildAsync_WithNegativeOrderBookDepth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = new MarketDepth("BTCUSDT");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: null, orderBookDepth: -5));
    }

    [Fact]
    public async Task BuildAsync_WithNegativeUpdateInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = new MarketDepth("BTCUSDT");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: TimeSpan.FromMilliseconds(-100)));
    }

    [Fact]
    public async Task BuildAsync_WithZeroUpdateInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = new MarketDepth("BTCUSDT");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: TimeSpan.Zero));
    }

    [Fact]
    public async Task StreamUpdates_WithNullMarketDepth_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            manager.StreamUpdatesAsync(null));
    }

    [Fact]
    public async Task StopStreamingAsync_WithoutActiveSubscription_DoesNotThrow()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act & Assert - should not throw
        await manager.StopStreamingAsync();
    }
}
