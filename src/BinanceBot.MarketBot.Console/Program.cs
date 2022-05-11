using System;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;

using static System.Console;


namespace BinanceBot.MarketBot.Console
{
    public class Program
    {
        // WARN: Set your credentials here here 
        private const string Key = "******";
        private const string Secret = "*****";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ApiCredentials Credentials = new (Key, Secret);


        static async Task Main(string[] args)
        {
            // 1. Set up bot settings
            const string token = "SOL/USDT"; // WARN: set up necessary token here

            var strategyConfig = new MarketStrategyConfiguration // WARN: set up trading strategy settings here
            {
                MinOrderVolume = 0.01M,
                MaxOrderVolume = 0.05M,
                TradeWhenSpreadGreaterThan = .05M
            };



            // 2. Init bot
            IBinanceClient binanceRestClient = new BinanceClient(
                new BinanceClientOptions { ApiCredentials = Credentials }
                );

            IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(
                new BinanceSocketClientOptions { ApiCredentials = Credentials }
                );

            IMarketBot bot = new MarketMakerBot(
                token.Replace("/", String.Empty),
                new NaiveMarketMakerStrategy(strategyConfig, Logger),
                binanceRestClient,
                binanceSocketClient,
                Logger);


            // 3. Run bot
            try
            {
                await bot.RunAsync();

                WriteLine("Press Enter to stop bot...");
                ReadLine();
            }
            finally
            {
                bot.Stop();
            }
           
            WriteLine("Press Enter to exit...");
            ReadLine();
        }   
    }
}
