using Binance.Net.Enums;

namespace BinanceBot.Market
{
    /// <summary>
    /// Request object used to create a new Binance order
    /// </summary>
    public class CreateOrderRequest
    {
        public string Symbol { get; set; }

        public OrderSide Side { get; set; }

        public SpotOrderType OrderType { get; set; }

        public TimeInForce? TimeInForce { get; set; }

        public decimal Quantity { get; set; }

        public decimal? Price { get; set; } 
        
        public string NewClientOrderId { get; set; }

        public decimal? StopPrice { get; set; }

        public decimal? IcebergQuantity { get; set; }
        public int? RecvWindow { get; set; }
    }
}