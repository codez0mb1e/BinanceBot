using System.Threading.Tasks;

using BinanceBot.Market;
using BinanceExchange.API.Client;
using BinanceExchange.API.Websockets;


namespace BinanceBot.MarketBot.Console
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public static async Task Main(string[] args)
        {
            // set bot settings
            const string token = "ETHBTC";

            IBinanceRestClient binanceRestClient = new BinanceRestClient(new BinanceClientConfiguration
            {
                ApiKey = "<your_api_key>", 
                SecretKey = "<your_secret_key>"
            });

            var strategyConfig = new MarketStrategyConfiguration
            {
                MinOrderVolume = 1.0M,
                MaxOrderVolume = 50.0M,
                TradeWhenSpreadGreaterThan = .02M
            };


            // create bot
            IMarketBot bot = new MarketMakerBot(
                token,
                new NaiveMarketMakerStrategy(strategyConfig, Logger),
                binanceRestClient,
                new BinanceWebSocketClient(binanceRestClient, Logger),
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
