using System.Collections.Generic;
using System.Linq;
using BinanceExchange.API.Enums;

namespace BinanceBot.Market.Utility
{
    internal static class QuoteExtensions
    {
        public static IEnumerable<Quote> ToQuotes(this IDictionary<decimal, decimal> source, OrderSide direction)
        {
            return source?.Select(s => new Quote(s.Key, s.Value, direction));
        }
    }
}