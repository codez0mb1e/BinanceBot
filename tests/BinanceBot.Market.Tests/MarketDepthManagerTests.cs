using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Interfaces.Clients;
using BinanceBot.Market.Domain;
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

    private static MarketDepth CreateTestMarketDepth() => 
        new MarketDepth(new MarketSymbol("BTC", "USDT", ContractType.Spot));

    [Test]
    public async Task Constructor_WithNullRestClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Task.FromResult(new MarketDepthManager(null, _mockSocketClient.Object, _mockLogger.Object)));
    }

    [Test]
    public async Task Constructor_WithNullSocketClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Task.FromResult(new MarketDepthManager(_mockRestClient.Object, null, _mockLogger.Object)));
    }

    [Test]
    public async Task Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Task.FromResult(new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, null)));
    }

    [Test]
    public async Task Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Assert
        await Assert.That(manager).IsNotNull();
    }

    [Test]
    public async Task BuildAsync_WithNullMarketDepth_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            manager.BuildAsync(null));
    }

    [Test]
    public async Task BuildAsync_WithZeroOrderBookDepth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: null, orderBookDepth: 0));
    }

    [Test]
    public async Task BuildAsync_WithNegativeOrderBookDepth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: null, orderBookDepth: -5));
    }

    [Test]
    public async Task BuildAsync_WithNegativeUpdateInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: TimeSpan.FromMilliseconds(-100)));
    }

    [Test]
    public async Task BuildAsync_WithZeroUpdateInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);
        var marketDepth = CreateTestMarketDepth();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            manager.BuildAsync(marketDepth, updateInterval: TimeSpan.Zero));
    }

    [Test]
    public async Task StreamUpdates_WithNullMarketDepth_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            manager.StreamUpdatesAsync(null));
    }

    [Test]
    public async Task StopStreamingAsync_WithoutActiveSubscription_DoesNotThrow()
    {
        // Arrange
        var manager = new MarketDepthManager(_mockRestClient.Object, _mockSocketClient.Object, _mockLogger.Object);

        // Act - should not throw
        await manager.StopStreamingAsync();
        
        // If we get here, no exception was thrown - test passes
    }
}
