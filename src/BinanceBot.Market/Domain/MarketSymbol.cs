
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
    /// <param name="baseAsset"></param>
    /// <param name="quoteAsset"></param>
    /// <param name="contractType"></param>
    /// <exception cref="ArgumentNullException">Thrown when baseAsset or quoteAsset is null.</exception>
    public MarketSymbol(string baseAsset, string quoteAsset, ContractType contractType)
    {
        BaseAsset = baseAsset ?? throw new ArgumentNullException(nameof(baseAsset));
        QuoteAsset = quoteAsset ?? throw new ArgumentNullException(nameof(quoteAsset));
        ContractType = contractType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketSymbol"/> class from a pair string.
    /// </summary>
    /// <param name="pair">The trading pair in the format "BASE/QUOTE".</param>
    /// <param name="contractType">The contract type (Spot or Futures). Defaults to Spot.</param>
    /// <exception cref="ArgumentException">Thrown when the pair format is invalid.</exception>
    public MarketSymbol(string pair, ContractType contractType = ContractType.Spot)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("Pair cannot be null or empty", nameof(pair));
        
        var assets = pair.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (assets.Length != 2)
            throw new ArgumentException("Pair must be in the format 'BASE/QUOTE'", nameof(pair));

        BaseAsset = assets[0];
        QuoteAsset = assets[1];
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
    /// The symbol name, can be used to overwrite the default formatted name
    /// </summary>
    public string FullName => $"{BaseAsset}{QuoteAsset}";

    /// <summary>
    /// The Contract type of the symbol
    /// </summary>
    public ContractType ContractType { get; init; }

    override public string ToString() => $"{BaseAsset}/{QuoteAsset} ({ContractType})";
}
