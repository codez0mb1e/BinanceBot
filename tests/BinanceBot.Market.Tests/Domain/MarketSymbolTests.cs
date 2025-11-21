using BinanceBot.Market.Domain;

namespace BinanceBot.Market.Tests.Domain;

public class MarketSymbolTests
{
    #region Constructor Tests - Three Parameters

    [Test]
    public async Task Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var symbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Assert
        await Assert.That(symbol.BaseAsset).IsEqualTo("BTC");
        await Assert.That(symbol.QuoteAsset).IsEqualTo("USDT");
        await Assert.That(symbol.ContractType).IsEqualTo(ContractType.Spot);
        await Assert.That(symbol.FullName).IsEqualTo("BTCUSDT");
        await Assert.That(symbol.ToString()).IsEqualTo("BTC/USDT (Spot)");
    }

    [Test]
    [Arguments("btc", "usdt", "BTC", "USDT")]
    [Arguments("BTC", "USDT", "BTC", "USDT")]
    [Arguments("Eth", "BtC", "ETH", "BTC")]
    [Arguments(" BNB ", " BUSD ", "BNB", "BUSD")]
    public async Task Constructor_NormalizesToUpperCase(string baseInput, string quoteInput, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(baseInput, quoteInput, ContractType.Spot);

        // Assert
        await Assert.That(symbol.BaseAsset).IsEqualTo(expectedBase);
        await Assert.That(symbol.QuoteAsset).IsEqualTo(expectedQuote);
    }

    [Test]
    public async Task Constructor_WithFuturesContractType_CreatesInstance()
    {
        // Act
        var symbol = new MarketSymbol("ETH", "USDT", ContractType.Futures);

        // Assert
        await Assert.That(symbol.ContractType).IsEqualTo(ContractType.Futures);
        await Assert.That(symbol.ToString()).IsEqualTo("ETH/USDT (Futures)");
    }

    [Test]
    public async Task Constructor_WithNullBaseAsset_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol(null, "USDT", ContractType.Spot)));
        await Assert.That(ex.ParamName).IsEqualTo("baseAsset");
    }

    [Test]
    public async Task Constructor_WithNullQuoteAsset_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol("BTC", null, ContractType.Spot)));
        await Assert.That(ex.ParamName).IsEqualTo("quoteAsset");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("\t")]
    public async Task Constructor_WithEmptyBaseAsset_ThrowsArgumentException(string emptyValue)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol(emptyValue, "USDT", ContractType.Spot)));
        await Assert.That(ex.ParamName).IsEqualTo("baseAsset");
        await Assert.That(ex.Message).Contains("empty or whitespace");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("\t")]
    public async Task Constructor_WithEmptyQuoteAsset_ThrowsArgumentException(string emptyValue)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol("BTC", emptyValue, ContractType.Spot)));
        await Assert.That(ex.ParamName).IsEqualTo("quoteAsset");
        await Assert.That(ex.Message).Contains("empty or whitespace");
    }

    #endregion

    #region Constructor Tests - Pair String

    [Test]
    [Arguments("BTC/USDT", "BTC", "USDT")]
    [Arguments("ETH/BTC", "ETH", "BTC")]
    [Arguments("BNB/BUSD", "BNB", "BUSD")]
    [Arguments("DOGE/USDT", "DOGE", "USDT")]
    public async Task Constructor_WithValidPair_CreatesInstance(string pair, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(pair);

        // Assert
        await Assert.That(symbol.BaseAsset).IsEqualTo(expectedBase);
        await Assert.That(symbol.QuoteAsset).IsEqualTo(expectedQuote);
        await Assert.That(symbol.ContractType).IsEqualTo(ContractType.Spot);
    }

    [Test]
    [Arguments("btc/usdt", "BTC", "USDT")]
    [Arguments("Eth/Btc", "ETH", "BTC")]
    [Arguments(" BNB / BUSD ", "BNB", "BUSD")]
    public async Task Constructor_WithPair_NormalizesToUpperCase(string pair, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(pair);

        // Assert
        await Assert.That(symbol.BaseAsset).IsEqualTo(expectedBase);
        await Assert.That(symbol.QuoteAsset).IsEqualTo(expectedQuote);
    }

    [Test]
    public async Task Constructor_WithPairAndFutures_CreatesInstance()
    {
        // Act
        var symbol = new MarketSymbol("BTC/USDT", ContractType.Futures);

        // Assert
        await Assert.That(symbol.BaseAsset).IsEqualTo("BTC");
        await Assert.That(symbol.QuoteAsset).IsEqualTo("USDT");
        await Assert.That(symbol.ContractType).IsEqualTo(ContractType.Futures);
    }

    [Test]
    [Arguments(null!)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Constructor_WithNullOrEmptyPair_ThrowsArgumentException(string? invalidPair)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol(invalidPair)));
        await Assert.That(ex.ParamName).IsEqualTo("pair");
        await Assert.That(ex.Message).Contains("null or empty");
    }

    [Test]
    [Arguments("BTCUSDT")]
    [Arguments("BTC")]
    [Arguments("BTC/USDT/EUR")]
    [Arguments("BTC-USDT")]
    public async Task Constructor_WithInvalidPairFormat_ThrowsArgumentException(string invalidPair)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol(invalidPair)));
        await Assert.That(ex.ParamName).IsEqualTo("pair");
        await Assert.That(ex.Message).Contains("BASE/QUOTE");
    }

    [Test]
    [Arguments("/USDT")]
    [Arguments("BTC/")]
    [Arguments(" / ")]
    public async Task Constructor_WithEmptyAssetInPair_ThrowsArgumentException(string invalidPair)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            Task.FromResult(new MarketSymbol(invalidPair)));
    }

    #endregion

    #region Property Tests

    [Test]
    public async Task FullName_ReturnsCorrectFormat()
    {
        // Arrange
        var symbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        await Assert.That(symbol.FullName).IsEqualTo("BTCUSDT");
    }

    [Test]
    public async Task ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var spotSymbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var futuresSymbol = new MarketSymbol("ETH", "BTC", ContractType.Futures);

        // Act & Assert
        await Assert.That(spotSymbol.ToString()).IsEqualTo("BTC/USDT (Spot)");
        await Assert.That(futuresSymbol.ToString()).IsEqualTo("ETH/BTC (Futures)");
    }

    #endregion

    #region Record Equality Tests

    [Test]
    public async Task Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        await Assert.That(symbol1).IsEqualTo(symbol2);
        await Assert.That(symbol1 == symbol2).IsTrue();
    }

    [Test]
    public async Task Equals_WithDifferentBaseAsset_ReturnsFalse()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("ETH", "USDT", ContractType.Spot);

        // Act & Assert
        await Assert.That(symbol1).IsNotEqualTo(symbol2);
        await Assert.That(symbol1 != symbol2).IsTrue();
    }

    [Test]
    public async Task Equals_WithDifferentContractType_ReturnsFalse()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Futures);

        // Act & Assert
        await Assert.That(symbol1).IsNotEqualTo(symbol2);
    }

    [Test]
    public async Task GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        await Assert.That(symbol1.GetHashCode()).IsEqualTo(symbol2.GetHashCode());
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task BothConstructors_WithSameData_CreateEqualInstances()
    {
        // Arrange & Act
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC/USDT", ContractType.Spot);

        // Assert
        await Assert.That(symbol1).IsEqualTo(symbol2);
    }

    [Test]
    public async Task BothConstructors_WithCaseInsensitiveInput_CreateEqualInstances()
    {
        // Arrange & Act
        var symbol1 = new MarketSymbol("btc", "usdt", ContractType.Futures);
        var symbol2 = new MarketSymbol("BTC/USDT", ContractType.Futures);

        // Assert
        await Assert.That(symbol1).IsEqualTo(symbol2);
    }

    #endregion
}
