
using System;

namespace BinanceBot.Market.Domain;

/// <summary>
/// A symbol representation based on a Base and Quote asset
/// </summary>
public record MarketSymbol
{
    public MarketSymbol(string baseAsset, string quoteAsset, ContractType contractType)
    {
        BaseAsset = baseAsset ?? throw new ArgumentNullException(nameof(baseAsset));
        QuoteAsset = quoteAsset ?? throw new ArgumentNullException(nameof(quoteAsset));
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
}


/// <summary>
///  Contract type
/// </summary>
public enum ContractType
{
    /// <summary>
    /// Spot market
    /// </summary>
    Spot,
    /// <summary>
    /// Perpetual Futures contract
    /// </summary>
    Perpetual
}