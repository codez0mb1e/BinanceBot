# Binance Bot
_Naive Market Maker Bot for Binance._

Solution contains two console projects:
- The `BinanceBot.MarketViewer.Console` project demonstrates Order Book updating in near-real time (via _Binance WebSocket API_). 
- The `BinanceBot.MarketBot.Console` project demonstrates how BinanceBot create and cancel orders (via _Binance REST API_) depends on current Market Depth.

Most of the `BinanceDotNet.BinanceExchange.API` project was taken from BinanceDotNet project [[2](#references)], but BinanceBot solution is no branch of BinanceDotNet project.

In picture below _BinanceBot create order to Order Book only if price spread by ETH/BTC greater than 0.2%_. 

![alt text][binance_bot_running]
[Full image](https://static.0xcode.in/images/binance_bot_running.png)

__Warn:__ BinanceBot uses _test order create_  API by default (without real order creation). 
Turn off `TEST_ORDER_CREATION_MODE` compilation symbol in [MarketMakerBot.cs](source/BinanceBot.Market/MarketMakerBot.cs) 
to _create real order_ in order book.


## Roadmap
#### Throlling
Binance has numerous limits to requests, orders count, min/max volume of order, etc. The networks restrictions should be taken into BinanceBot working as part of `BinanceClientConfiguration`, 
and the orders restrictions - as part of `MarkerStrategyConfiguration`.

#### Security
Binance API keys should be stored in secured storage (such as `Azure Key Vault` service) instead of plain text such it is now.

#### Configurations
Any configuration (of connectors or bot) should be placed in separate configuration storage (such as `JSON files` in local file system).
This will allow you to reconfigure the bot without the need for recompilation.

#### Other
BinanceBot doesn’t processed network connection errors. 
It would be to implement `Retry Policies` for the broken connections and handling other network errors. 

The solution doesn’t contain any `Unit Tests`/`Integration Tests`, which is a bad practice. 

## Requirements
__.NET Core 2.1__ (also compatible with .NET 4.5.1, .NET 4.5.2, .NET 4.6.1, .NETSTANDARD2.0).


## References
1. [Binance official API docs](https://github.com/binance-exchange/binance-official-api-docs).
2. [Official C# Wrapper for the Binance exchange API](https://github.com/glitch100/BinanceDotNet).

[binance_bot_running]: https://static.0xcode.in/images/binance_bot_running.png "binance bot"