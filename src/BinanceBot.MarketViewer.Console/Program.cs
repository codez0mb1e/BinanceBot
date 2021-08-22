using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

namespace BinanceBot.MarketViewer.Console
{
    public class Program
    {
        private const string Key = "";
        private const string Secret = "";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public static async Task Main(string[] args)
        {
            const string token = "ETHBTC";

            IBinanceClient binanceRestClient = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Key, Secret)
            });


            var marketDepth = new MarketDepth(token);

            await TestConnection(binanceRestClient);

            marketDepth.MarketDepthChanged += (sender, e) =>
            {
                int n = 20;

                System.Console.Clear();

                System.Console.WriteLine("Price : Volume");
                System.Console.WriteLine(
                    JsonConvert.SerializeObject(
                        new
                        {
                            LastUpdate = e.UpdateTime,
                            Asks = e.Asks.Take(n).Reverse().Select(s => $"{s.Price} : {s.Volume}"),
                            Bids = e.Bids.Take(n).Select(s => $"{s.Price} : {s.Volume}")
                        }, 
                        Formatting.Indented));

                System.Console.WriteLine("Press Enter to stop streaming market depth...");

                System.Console.SetCursorPosition(0, 0);
            };


            IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions());
            binanceSocketClient.SetApiCredentials(Key, Secret);

            var marketDepthManager = new MarketDepthManager(binanceRestClient, binanceSocketClient);

            // build order book
            await marketDepthManager.BuildAsync(marketDepth);
            // stream order book updates
            marketDepthManager.StreamUpdates(marketDepth);


            System.Console.WriteLine("Press Enter to exit...");
            System.Console.ReadLine();
        }



        private static async Task TestConnection(IBinanceClient binanceRestClient)
        {
            Logger.Info("Testing connection...");

            var testConnectResponse = await binanceRestClient.PingAsync();
            DateTime serverTimeResponse = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds(testConnectResponse.Data / 10.0)
                .ToLocalTime();

            Logger.Info($"Connection was established successfully. Approximate ping time: {DateTime.UtcNow.Subtract(serverTimeResponse).TotalMilliseconds:F0} ms");
        }
    }
}
