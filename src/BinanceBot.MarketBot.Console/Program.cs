using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using BinanceBot.Market;
using CryptoExchange.Net.Authentication;

using static System.Console;


namespace BinanceBot.MarketBot.Console
{
    public class Program
    {
        #region Bot Settings
        // WARN: Set your credentials here here 
        private const string ApiKey = "***";
        private const string Secret = "***";
        
        // WARN: Set necessary token here
        private const string Symbol = "BNBUSDT";
        private static TimeSpan ReceiveWindow = TimeSpan.FromMinutes(1000);
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
            var pingAsyncResult = await binanceRestClient.PingAsync();
            WriteLine($"Ping time: {pingAsyncResult.Data} ms");


            // 3. set bot strategy config
            var exchangeInfoResult = 
                binanceRestClient.Spot.System.GetExchangeInfoAsync(Symbol);

            var symbolInfo = exchangeInfoResult.Result.Data.Symbols
                .Single(s => s.Name.Equals(Symbol, StringComparison.InvariantCultureIgnoreCase));

            if (!(symbolInfo.Status == SymbolStatus.Trading && symbolInfo.OrderTypes.Contains(OrderType.Market)))
            {
                WriteLine($"[ERROR] Symbol {symbolInfo.Name} doesn't suitable for this strategy.");
                return;
            }

            if (symbolInfo.LotSizeFilter == null)
            {
                WriteLine($"[ERROR] Cannot define risks strategy for {symbolInfo.Name}.");
                return;
            }
            
            var strategyConfig = new MarketStrategyConfiguration
            {
                MinOrderVolume = symbolInfo.LotSizeFilter.MinQuantity,
                MaxOrderVolume = symbolInfo.LotSizeFilter.MinQuantity*100,
                TradeWhenSpreadGreaterThan = .02M,
                ReceiveWindow = ReceiveWindow
            };


            // 3. Start bot
            IMarketBot bot = new MarketMakerBot(
                Symbol,
                new NaiveMarketMakerStrategy(strategyConfig, Logger),
                binanceRestClient,
                binanceSocketClient,
                Logger);

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
