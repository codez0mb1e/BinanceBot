using System;

namespace BinanceExchange.API.Client
{
    public class BinanceClientConfiguration
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public bool EnableRateLimiting { get; set; }
        public TimeSpan CacheTime { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan TimestampOffset { get; set; } = TimeSpan.FromMilliseconds(0);
        public int DefaultReceiveWindow { get; set; } = 5000;
    }
}