using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using BinanceBot.Market;
using BinanceBot.Market.Core;
using dotenv.net;
using Spectre.Console;

using static System.Console;


namespace BinanceBot.MarketViewer.Console
{
    internal static class Program
    {
        #region Bot Settings
        // WARN: Set necessary token here
        private const string Symbol = "ETHUSDT";
        private const int OrderBookDepth = 10;
        private static readonly TimeSpan? OrderBookUpdateLimit = TimeSpan.FromMilliseconds(1000);
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        static async Task Main(string[] args)
        {
            // Load .env file first
            DotEnv.Load();

            // WARN: Set your credentials in .env file
            var apiKey = Environment.GetEnvironmentVariable("BINANCE_API_KEY") ?? throw new InvalidOperationException("BINANCE_API_KEY environment variable is not set");
            var secret = Environment.GetEnvironmentVariable("BINANCE_SECRET") ?? throw new InvalidOperationException("BINANCE_SECRET environment variable is not set");

            // 1. create connections with exchange
            var credentials = new BinanceApiCredentials(apiKey, secret);
            using IBinanceClient binanceRestClient = new BinanceClient(new BinanceClientOptions { ApiCredentials = credentials });
            using IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions { ApiCredentials = credentials });


            // 2. test connection
            await AnsiConsole.Status()
                .StartAsync("Testing connection...", async ctx =>
                {
                    var pingResult = await binanceRestClient.SpotApi.ExchangeData.PingAsync();
                    AnsiConsole.MarkupLine($"Ping time: [yellow]{pingResult.Data} ms[/]");

                    Task.Delay(1000).Wait();
                });

            // 3. get order book
            var marketDepthManager = new MarketDepthManager(binanceRestClient, binanceSocketClient);
            var marketDepth = new MarketDepth(Symbol);


            // 4. Render order book
            var orderBookTable = new Table
            {
                Title = new TableTitle($"{Symbol} Quotes")
            };

            foreach (var column in new[] { "Asks (volume)", "Price", "Bid (volume)" })
                orderBookTable.AddColumn(column);

            static IEnumerable<(string price, string volume)> GetValues(IEnumerable<Quote> data)
            {
                return data
                    .OrderByDescending(q => q.Price)
                    .Select(q => (price: q.Price.ToString(CultureInfo.InvariantCulture), volume: q.Volume.ToString(CultureInfo.InvariantCulture)) );
            }

            marketDepth.MarketDepthChanged += (sender, e) =>
            {
                var asks = e.Asks.OrderBy(q => q.Price).Take(OrderBookDepth).ToImmutableArray();
                var bids = e.Bids.OrderByDescending(q => q.Price).Take(OrderBookDepth).ToImmutableArray();


                orderBookTable.Rows.Clear();

                foreach (var row in GetValues(asks))
                    orderBookTable.AddRow(row.volume, $"[red]{row.price}[/]", String.Empty);

                foreach (var row in GetValues(bids))
                    orderBookTable.AddRow(String.Empty, $"[green]{row.price}[/]", row.volume);

                orderBookTable.Caption = new TableTitle(
                    $"Spread: {e.Asks.Select(q => q.Price).Min() - e.Bids.Select(q => q.Price).Max()}. " +
                    $"Last updated as {DateTimeOffset.FromUnixTimeSeconds(e.UpdateTime):T}\n"
                    );

                var dominanceChart = new BreakdownChart()
                    .ShowTagValues()
                    .AddItem("Asks", (double) asks.Select(q => q.Volume).Sum(), Color.Red)
                    .AddItem("Bids", (double) bids.Select(q => q.Volume).Sum(), Color.Green);


                AnsiConsole.Clear();

                AnsiConsole.Write(orderBookTable);
                AnsiConsole.Write(dominanceChart);
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
