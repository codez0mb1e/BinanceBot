using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

using static System.Console;


namespace BinanceBot.MarketViewer.Console
{
    public class Program
    {
        // WARN: Set your credentials here here 
        private const string Key = "******";
        private const string Secret = "*****";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ApiCredentials Credentials = new(Key, Secret);


        static async Task Main(string[] args)
        {
            const string token = "SOLUSDT"; // WARN: Set necessary token here

            IBinanceClient binanceRestClient = new BinanceClient(
                new BinanceClientOptions { ApiCredentials = Credentials }
            );


            var marketDepth = new MarketDepth(token);

            await TestConnectionAsync(binanceRestClient);


            const int marketDepthLimit = 10;
            marketDepth.MarketDepthChanged += (sender, e) =>
            {
                Clear();

                WriteLine("Price : Volume");
                WriteLine(
                    JsonConvert.SerializeObject(
                        new
                        {
                            LastUpdate = e.UpdateTime,
                            Asks = e.Asks.Take(marketDepthLimit).Reverse().Select(s => $"{s.Price} : {s.Volume}"),
                            Bids = e.Bids.Take(marketDepthLimit).Select(s => $"{s.Price} : {s.Volume}")
                        }, 
                        Formatting.Indented));

                WriteLine("Press Enter to stop streaming market depth...");

                SetCursorPosition(0, 0);
            };


            IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(
                new BinanceSocketClientOptions { ApiCredentials = Credentials }
            );

            var marketDepthManager = new MarketDepthManager(binanceRestClient, binanceSocketClient);

            // build order book
            await marketDepthManager.BuildAsync(marketDepth);
            // stream order book updates
            marketDepthManager.StreamUpdates(marketDepth);


            WriteLine("Press Enter to exit...");
            ReadLine();
        }



        private static async Task TestConnectionAsync(IBinanceClient binanceRestClient, CancellationToken ct = default)
        {
            if (binanceRestClient == null) throw new ArgumentNullException(nameof(binanceRestClient));

            Logger.Info("Testing connection...");

            var testConnectResponse = await binanceRestClient.SpotApi.ExchangeData.PingAsync(ct).ConfigureAwait(false);

            if (testConnectResponse.Error != null)
                Logger.Error(testConnectResponse.Error.Message);
            else
            {
                string msg = $"Connection was established successfully. Approximate ping time: {testConnectResponse.Data} ms";
                if (testConnectResponse.Data > 1000)
                    Logger.Warn(msg);
                else
                    Logger.Info(msg);
            }
        }
    }
}
