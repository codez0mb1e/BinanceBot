using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

using static System.Console;


namespace BinanceBot.MarketViewer.Console
{
    public class Program
    {
        #region Bot Settings
        // WARN: Set your credentials here here 
        private const string ApiKey = "***";
        private const string Secret = "***";

        // WARN: Set necessary token here
        private const string Symbol = "BNBUSDT";
        private const int OrderBookDepth = 10;
        private static readonly TimeSpan? OrderBookUpdateLimit = TimeSpan.FromMilliseconds(1000);
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        static async Task Main(string[] args)
        {
            // 1. create connections with exchange
            var credentials = new ApiCredentials(ApiKey, Secret);
            using IBinanceClient binanceRestClient = new BinanceClient(new BinanceClientOptions { ApiCredentials = credentials });
            using IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions { ApiCredentials = credentials });


            // 2. test connection
            WriteLine("Testing connection...");
            var pingResult = await binanceRestClient.PingAsync();
            WriteLine($"Ping time: {pingResult.Data} ms");


            // 3. get order book
            var marketDepthManager = new MarketDepthManager(binanceRestClient, binanceSocketClient);
            var marketDepth = new MarketDepth(Symbol);

 
            marketDepth.MarketDepthChanged += (sender, e) =>
            {
                Clear();

                WriteLine("Price : Volume");

                WriteLine(
                    JsonConvert.SerializeObject(
                        new
                        {
                            LastUpdate = e.UpdateTime,
                            Asks = e.Asks.Reverse().Take(OrderBookDepth).Select(s => $"{s.Price} : {s.Volume}"),
                            Bids = e.Bids.Take(OrderBookDepth).Select(s => $"{s.Price} : {s.Volume}")
                        }, 
                        Formatting.Indented));

                WriteLine("Press Enter to stop streaming market depth...");

                SetCursorPosition(0, 0);
            };


            // build order book
            await marketDepthManager.BuildAsync(marketDepth, OrderBookDepth);
            // stream order book updates
            marketDepthManager.StreamUpdates(marketDepth, OrderBookUpdateLimit);


            WriteLine("Press Enter to exit...");
            ReadLine();
        }
    }
}
