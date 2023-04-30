# Market Bot for Binance

[![Status](https://img.shields.io/badge/status-in_active_development-green.svg)](https://github.com/codez0mb1e/BinanceBot/projects/1)
[![Contributors Welcome](https://img.shields.io/badge/contributing-welcome-blue.svg)](CONTRIBUTING.md)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Naive _Market Maker Bot_ for Binance exchange.

Solution contains two console projects:

- The `BinanceBot.MarketViewer.Console` project: __Order book updating in near-real time__ (via _Binance WebSocket API_).
- The `BinanceBot.MarketBot.Console` project: __Create and cancel orders__ (via _Binance REST API_) depends on current Market Depth.

In picture below _BinanceBot create order to Order Book only if price spread by ETH/BTC greater than 0.2%_.

![alt text][binance_bot_running]

__Warn:__ BinanceBot uses _test order create_  API by default (without real order creation).
Turn off `TEST_ORDER_CREATION_MODE` compilation symbol in [MarketMakerBot.cs](src/BinanceBot.Market/MarketMakerBot.cs) to _create real order_ in order book.

## Requirements

- .NET 7.0
- Binance Account.

## References

1. [Binance official API docs](https://github.com/binance-exchange/binance-official-api-docs).
1. [Official C# Wrapper for the Binance exchange API](https://github.com/glitch100/BinanceDotNet).

[binance_bot_running]: https://static.0xcode.in/images/binance_bot_running.png "binance bot"
