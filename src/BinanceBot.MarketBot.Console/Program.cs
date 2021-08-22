using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;


namespace BinanceBot.MarketBot.Console
{
    public class Program
    {
        private const string Key = "";
        private const string Secret = "";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public static async Task Main(string[] args)
        {
            // set bot settings
            const string token = "ETHBTC";

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

                System.Console.WriteLine("Press Enter to stop bot...");
                System.Console.ReadLine();
            }
            finally
            {
                bot.Stop();
            }
           
            System.Console.WriteLine("Press Enter to exit...");
            System.Console.ReadLine();
        }   
    }
}
