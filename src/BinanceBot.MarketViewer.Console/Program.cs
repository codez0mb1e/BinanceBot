using System;
using System.Linq;
using System.Threading.Tasks;
using BinanceBot.Market;
using BinanceExchange.API.Client;

using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Websockets;
using Newtonsoft.Json;

namespace BinanceBot.MarketViewer.Console
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public static async Task Main(string[] args)
        {
            const string token = "ETHBTC";

            IBinanceRestClient binanceRestClient = new BinanceRestClient(new BinanceClientConfiguration
            {
                ApiKey = "<your_api_key>",
                SecretKey = "<your_secret_key>"
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


            var marketDepthManager = new MarketDepthManager(binanceRestClient, new BinanceWebSocketClient(binanceRestClient, Logger));

            // build order book
            await marketDepthManager.BuildAsync(marketDepth);
            // stream order book updates
            marketDepthManager.StreamUpdates(marketDepth);


            System.Console.WriteLine("Press Enter to exit...");
            System.Console.ReadLine();
        }



        private static async Task TestConnection(IBinanceRestClient binanceRestClient)
        {
            Logger.Info("Testing connection...");
            IResponse testConnectResponse = await binanceRestClient.TestConnectivityAsync();
            if (testConnectResponse != null)
            {
                ServerTimeResponse serverTimeResponse = await binanceRestClient.GetServerTimeAsync();
                Logger.Info($"Connection is established. Approximate ping time: {DateTime.UtcNow.Subtract(serverTimeResponse.ServerTime).TotalMilliseconds:F0} ms");
            }
        }
    }
}
