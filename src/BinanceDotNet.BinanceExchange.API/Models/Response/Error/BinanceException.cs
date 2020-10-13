using System;

namespace BinanceExchange.API.Models.Response.Error
{
    public class BinanceException : Exception
    {
        public BinanceException(string message, BinanceError errorDetails) : base(message)
        {
            ErrorDetails = errorDetails;
        }

        public BinanceError ErrorDetails { get; set; }
    }
}