using BinanceBot.Market.Domain;

namespace BinanceBot.Market.Tests.Domain;

public class MarketSymbolTests
{
    #region Constructor Tests - Three Parameters

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var symbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Assert
        Assert.Equal("BTC", symbol.BaseAsset);
        Assert.Equal("USDT", symbol.QuoteAsset);
        Assert.Equal(ContractType.Spot, symbol.ContractType);
        Assert.Equal("BTCUSDT", symbol.FullName);
        Assert.Equal("BTC/USDT (Spot)", symbol.ToString());
    }

    [Theory]
    [InlineData("btc", "usdt", "BTC", "USDT")]
    [InlineData("BTC", "USDT", "BTC", "USDT")]
    [InlineData("Eth", "BtC", "ETH", "BTC")]
    [InlineData(" BNB ", " BUSD ", "BNB", "BUSD")]
    public void Constructor_NormalizesToUpperCase(string baseInput, string quoteInput, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(baseInput, quoteInput, ContractType.Spot);

        // Assert
        Assert.Equal(expectedBase, symbol.BaseAsset);
        Assert.Equal(expectedQuote, symbol.QuoteAsset);
    }

    [Fact]
    public void Constructor_WithFuturesContractType_CreatesInstance()
    {
        // Act
        var symbol = new MarketSymbol("ETH", "USDT", ContractType.Futures);

        // Assert
        Assert.Equal(ContractType.Futures, symbol.ContractType);
        Assert.Equal("ETH/USDT (Futures)", symbol.ToString());
    }

    [Fact]
    public void Constructor_WithNullBaseAsset_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol(null, "USDT", ContractType.Spot));
        Assert.Equal("baseAsset", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullQuoteAsset_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol("BTC", null, ContractType.Spot));
        Assert.Equal("quoteAsset", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithEmptyBaseAsset_ThrowsArgumentException(string emptyValue)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol(emptyValue, "USDT", ContractType.Spot));
        Assert.Equal("baseAsset", ex.ParamName);
        Assert.Contains("empty or whitespace", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithEmptyQuoteAsset_ThrowsArgumentException(string emptyValue)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol("BTC", emptyValue, ContractType.Spot));
        Assert.Equal("quoteAsset", ex.ParamName);
        Assert.Contains("empty or whitespace", ex.Message);
    }

    #endregion

    #region Constructor Tests - Pair String

    [Theory]
    [InlineData("BTC/USDT", "BTC", "USDT")]
    [InlineData("ETH/BTC", "ETH", "BTC")]
    [InlineData("BNB/BUSD", "BNB", "BUSD")]
    [InlineData("DOGE/USDT", "DOGE", "USDT")]
    public void Constructor_WithValidPair_CreatesInstance(string pair, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(pair);

        // Assert
        Assert.Equal(expectedBase, symbol.BaseAsset);
        Assert.Equal(expectedQuote, symbol.QuoteAsset);
        Assert.Equal(ContractType.Spot, symbol.ContractType);
    }

    [Theory]
    [InlineData("btc/usdt", "BTC", "USDT")]
    [InlineData("Eth/Btc", "ETH", "BTC")]
    [InlineData(" BNB / BUSD ", "BNB", "BUSD")]
    public void Constructor_WithPair_NormalizesToUpperCase(string pair, string expectedBase, string expectedQuote)
    {
        // Act
        var symbol = new MarketSymbol(pair);

        // Assert
        Assert.Equal(expectedBase, symbol.BaseAsset);
        Assert.Equal(expectedQuote, symbol.QuoteAsset);
    }

    [Fact]
    public void Constructor_WithPairAndFutures_CreatesInstance()
    {
        // Act
        var symbol = new MarketSymbol("BTC/USDT", ContractType.Futures);

        // Assert
        Assert.Equal("BTC", symbol.BaseAsset);
        Assert.Equal("USDT", symbol.QuoteAsset);
        Assert.Equal(ContractType.Futures, symbol.ContractType);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyPair_ThrowsArgumentException(string? invalidPair)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol(invalidPair));
        Assert.Equal("pair", ex.ParamName);
        Assert.Contains("null or empty", ex.Message);
    }

    [Theory]
    [InlineData("BTCUSDT")]
    [InlineData("BTC")]
    [InlineData("BTC/USDT/EUR")]
    [InlineData("BTC-USDT")]
    public void Constructor_WithInvalidPairFormat_ThrowsArgumentException(string invalidPair)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            new MarketSymbol(invalidPair));
        Assert.Equal("pair", ex.ParamName);
        Assert.Contains("BASE/QUOTE", ex.Message);
    }

    [Theory]
    [InlineData("/USDT")]
    [InlineData("BTC/")]
    [InlineData(" / ")]
    public void Constructor_WithEmptyAssetInPair_ThrowsArgumentException(string invalidPair)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new MarketSymbol(invalidPair));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void FullName_ReturnsCorrectFormat()
    {
        // Arrange
        var symbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        Assert.Equal("BTCUSDT", symbol.FullName);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var spotSymbol = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var futuresSymbol = new MarketSymbol("ETH", "BTC", ContractType.Futures);

        // Act & Assert
        Assert.Equal("BTC/USDT (Spot)", spotSymbol.ToString());
        Assert.Equal("ETH/BTC (Futures)", futuresSymbol.ToString());
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        Assert.Equal(symbol1, symbol2);
        Assert.True(symbol1 == symbol2);
    }

    [Fact]
    public void Equals_WithDifferentBaseAsset_ReturnsFalse()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("ETH", "USDT", ContractType.Spot);

        // Act & Assert
        Assert.NotEqual(symbol1, symbol2);
        Assert.True(symbol1 != symbol2);
    }

    [Fact]
    public void Equals_WithDifferentContractType_ReturnsFalse()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Futures);

        // Act & Assert
        Assert.NotEqual(symbol1, symbol2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC", "USDT", ContractType.Spot);

        // Act & Assert
        Assert.Equal(symbol1.GetHashCode(), symbol2.GetHashCode());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void BothConstructors_WithSameData_CreateEqualInstances()
    {
        // Arrange & Act
        var symbol1 = new MarketSymbol("BTC", "USDT", ContractType.Spot);
        var symbol2 = new MarketSymbol("BTC/USDT", ContractType.Spot);

        // Assert
        Assert.Equal(symbol1, symbol2);
    }

    [Fact]
    public void BothConstructors_WithCaseInsensitiveInput_CreateEqualInstances()
    {
        // Arrange & Act
        var symbol1 = new MarketSymbol("btc", "usdt", ContractType.Futures);
        var symbol2 = new MarketSymbol("BTC/USDT", ContractType.Futures);

        // Assert
        Assert.Equal(symbol1, symbol2);
    }

    #endregion
}
