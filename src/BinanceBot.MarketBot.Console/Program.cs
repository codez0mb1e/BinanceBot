using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using BinanceBot.Market;
using BinanceBot.Market.Configurations;
using BinanceBot.Market.Strategies;
using CryptoExchange.Net.Authentication;

using static System.Console;


namespace BinanceBot.MarketBot.Console
{
    internal static class Program
    {
        #region Bot Settings
        // WARN: Set your credentials here here 
        private const string ApiKey = "***";
        private const string Secret = "***";

        // WARN: Set necessary token here
        private const string Symbol = "DOGEBTC";
        private static readonly TimeSpan ReceiveWindow = TimeSpan.FromMilliseconds(1000);
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        

        static async Task Main(string[] args)
        {
            // 1. create connections with exchange
            var credentials = new ApiCredentials(ApiKey, Secret);
            using IBinanceClient binanceRestClient = new BinanceClient(new BinanceClientOptions { ApiCredentials = credentials });
            using IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions { ApiCredentials = credentials });


            // 2. test connection
            Logger.Info("Testing connection...");
            var pingAsyncResult = await binanceRestClient.PingAsync();
            Logger.Info($"Ping: {pingAsyncResult.Data} ms");


            // 2.1. check permissions
            var permissionsResponse = await binanceRestClient.General.GetAPIKeyPermissionsAsync();
            if (!permissionsResponse.Success)
            {
                Logger.Error($"{permissionsResponse.Error?.Message}");
                ReadLine();
            }
            else if (permissionsResponse.Data.IpRestrict | !permissionsResponse.Data.EnableSpotAndMarginTrading)
            {
                Logger.Error("Insufficient API permissions.");
                ReadLine();
            }


            // 3. set bot strategy config
            var exchangeInfoResult = 
                binanceRestClient.Spot.System.GetExchangeInfoAsync(Symbol);

            var symbolInfo = exchangeInfoResult.Result.Data.Symbols
                .Single(s => s.Name.Equals(Symbol, StringComparison.InvariantCultureIgnoreCase));

            if (!(symbolInfo.Status == SymbolStatus.Trading && symbolInfo.OrderTypes.Contains(OrderType.Market)))
            {
                Logger.Error($"Symbol {symbolInfo.Name} doesn't suitable for this strategy.");
                return;
            }

            if (symbolInfo.LotSizeFilter == null)
            {
                Logger.Error($"Cannot define risks strategy for {symbolInfo.Name}.");
                return;
            }

            // WARN: Set thresholds for strategy here
            var strategyConfig = new MarketStrategyConfiguration
            {
                MinOrderVolume = symbolInfo.LotSizeFilter.MinQuantity,
                MaxOrderVolume = symbolInfo.LotSizeFilter.MinQuantity*10,
                TradeWhenSpreadGreaterThan = .02M, // == 0.02%
                QuoteAssetPrecision = symbolInfo.QuoteAssetPrecision,
                ReceiveWindow = ReceiveWindow
            };

            var marketStrategy = new NaiveMarketMakerStrategy(strategyConfig, Logger);


            // 3. Start bot
            IMarketBot bot = new MarketMakerBot(
                Symbol,
                marketStrategy,
                binanceRestClient,
                binanceSocketClient,
                Logger);

            try
            {
                await bot.RunAsync();

                WriteLine($"Press Enter to stop {Symbol} bot...");
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
