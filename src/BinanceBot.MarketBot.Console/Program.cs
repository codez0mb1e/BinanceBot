using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using BinanceBot.Market;
using BinanceBot.Market.Configurations;
using BinanceBot.Market.Strategies;
using dotenv.net;

using static System.Console;


namespace BinanceBot.MarketBot.Console;

internal static class Program
{
    #region Bot Settings
    // WARN: set necessary token here
    private const string Symbol = "BNBUSDT";
    private static readonly TimeSpan ReceiveWindow = TimeSpan.FromMilliseconds(100);
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
        Logger.Info("Testing connection...");
        var pingAsyncResult = await binanceRestClient.SpotApi.ExchangeData.PingAsync();
        Logger.Info($"Ping {pingAsyncResult.Data} ms");


        // 2.1. check permissions
        var permissionsResponse = await binanceRestClient.SpotApi.Account.GetAPIKeyPermissionsAsync();
        if (!permissionsResponse.Success)
        {
            Logger.Error($"{permissionsResponse.Error?.Message}");
            ReadLine();
        }
        
        if (!(permissionsResponse.Data.IpRestrict && permissionsResponse.Data.EnableSpotAndMarginTrading))
        {
            Logger.Error("Insufficient API permissions");
            ReadLine();
        }


        // 3. set bot strategy config
        var exchangeInfoResult = binanceRestClient.SpotApi.ExchangeData.GetExchangeInfoAsync(Symbol);

        var symbolInfo = exchangeInfoResult.Result.Data.Symbols
            .Single(s => s.Name.Equals(Symbol, StringComparison.InvariantCultureIgnoreCase));

        if (!(symbolInfo.Status == SymbolStatus.Trading && symbolInfo.OrderTypes.Contains(SpotOrderType.Market)))
        {
            Logger.Error($"Symbol {symbolInfo.Name} doesn't suitable for this strategy");
            return;
        }

        if (symbolInfo.LotSizeFilter == null)
        {
            Logger.Error($"Cannot define risks strategy for {symbolInfo.Name}");
            return;
        }

        if (symbolInfo.PriceFilter == null)
        {
            Logger.Error($"Cannot define price precision for {symbolInfo.Name}. Please define it manually.");
            return;
        }
        int pricePrecision = (int)Math.Abs(Math.Log10(Convert.ToDouble(symbolInfo.PriceFilter.TickSize)));


        // WARN: set thresholds for strategy here
        var strategyConfig = new MarketStrategyConfiguration
        {
            TradeWhenSpreadGreaterThan = .05M, // or 0.05%, (price spread*min_volume) should be greater than broker's commissions for trade
            MinOrderVolume = symbolInfo.LotSizeFilter.MinQuantity*10,
            MaxOrderVolume = symbolInfo.LotSizeFilter.MinQuantity*100,
            BaseAssetPrecision = symbolInfo.BaseAssetPrecision,
            PricePrecision = pricePrecision,
            ReceiveWindow = ReceiveWindow
        };

        var marketStrategy = new NaiveMarketMakerStrategy(strategyConfig, Logger);


        // 3. start bot
        IMarketBot bot = new MarketMakerBot(Symbol, marketStrategy, binanceRestClient, binanceSocketClient, Logger);

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
