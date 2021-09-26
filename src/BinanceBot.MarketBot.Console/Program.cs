using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Interfaces;
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


        static async Task Main(string[] args)
        {
            // set bot settings
            const string token = "ETHBTC"; // WARN: Set necessary token here

            IBinanceClient binanceRestClient = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Key, Secret)
            });

            var strategyConfig = new MarketStrategyConfiguration
            {
                MinOrderVolume = 0.0001M,
                MaxOrderVolume = 0.001M,
                TradeWhenSpreadGreaterThan = .001M
            };


            // create bot
            IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions());
            binanceSocketClient.SetApiCredentials(Key, Secret);

            IMarketBot bot = new MarketMakerBot(
                token,
                new NaiveMarketMakerStrategy(strategyConfig, Logger),
                binanceRestClient,
                binanceSocketClient,
                Logger);


            // start bot
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
