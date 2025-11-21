
using System;

namespace BinanceBot.Market.Domain;


/// <summary>
/// Market Contract type
/// </summary>
public enum ContractType
{
    /// <summary>
    /// Spot market
    /// </summary>
    Spot,
    /// <summary>
    /// Futures contract
    /// </summary>
    Futures
}


/// <summary>
/// A market symbol representation based on a Base and Quote asset and Contract type
/// </summary>
public record MarketSymbol
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketSymbol"/> class.
    /// </summary>
    /// <param name="baseAsset">The base asset of the trading pair (e.g., "BTC", "ETH")</param>
    /// <param name="quoteAsset">The quote asset of the trading pair (e.g., "USDT", "BTC")</param>
    /// <param name="contractType">The contract type (Spot or Futures)</param>
    /// <exception cref="ArgumentException">Thrown when baseAsset or quoteAsset is null, empty or whitespace.</exception>
    public MarketSymbol(string baseAsset, string quoteAsset, ContractType contractType)
    {
        if (string.IsNullOrWhiteSpace(baseAsset))
            throw new ArgumentException("Base asset cannot be empty or whitespace", nameof(baseAsset));
        if (string.IsNullOrWhiteSpace(quoteAsset))
            throw new ArgumentException("Quote asset cannot be empty or whitespace", nameof(quoteAsset));
        
        BaseAsset = baseAsset.Trim().ToUpperInvariant();
        QuoteAsset = quoteAsset.Trim().ToUpperInvariant();
        ContractType = contractType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketSymbol"/> class from a pair string.
    /// </summary>
    /// <param name="pair">The trading pair in the format "BASE/QUOTE" (e.g., "BTC/USDT", "ETH/BTC")</param>
    /// <param name="contractType">The contract type (Spot or Futures). Defaults to Spot.</param>
    /// <exception cref="ArgumentException">Thrown when the pair is null, empty, or not in the correct format.</exception>
    public MarketSymbol(string pair, ContractType contractType = ContractType.Spot)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("Pair cannot be null or empty", nameof(pair));
        
        var assets = pair.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (assets.Length != 2)
            throw new ArgumentException("Pair must be in the format 'BASE/QUOTE' (e.g., 'BTC/USDT')", nameof(pair));
        
        if (string.IsNullOrWhiteSpace(assets[0]))
            throw new ArgumentException("Base asset cannot be empty", nameof(pair));
        if (string.IsNullOrWhiteSpace(assets[1]))
            throw new ArgumentException("Quote asset cannot be empty", nameof(pair));

        BaseAsset = assets[0].ToUpperInvariant();
        QuoteAsset = assets[1].ToUpperInvariant();
        ContractType = contractType;
    }

    /// <summary>
    /// The base asset of the symbol
    /// </summary>
    public string BaseAsset { get; init; }

    /// <summary>
    /// The quote asset of the symbol
    /// </summary>
    public string QuoteAsset { get; init; }

    /// <summary>
    /// The symbol name in Binance API format (e.g., "BTCUSDT")
    /// </summary>
    public string FullName => $"{BaseAsset}{QuoteAsset}";

    /// <summary>
    /// The contract type of the symbol (Spot or Futures)
    /// </summary>
    public ContractType ContractType { get; init; }

    /// <summary>
    /// Returns a string representation in the format "BASE/QUOTE (ContractType)" (e.g., "BTC/USDT (Spot)")
    /// </summary>
    public override string ToString() => $"{BaseAsset}/{QuoteAsset} ({ContractType})";
}
