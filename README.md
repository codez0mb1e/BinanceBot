# Market Bot for Binance

[![Contributors Welcome](https://img.shields.io/badge/contributing-welcome-blue.svg)](CONTRIBUTING.md)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Naive _Market Maker Bot_ for Binance exchange.

Solution contains two console projects:

- The `BinanceBot.MarketViewer.Console` project: __Order book updating in near-real time__ (via _Binance WebSocket API_).
- The `BinanceBot.MarketBot.Console` project: __Create and cancel orders__ (via _Binance REST API_) depends on current Market Depth.

![alt text](/docs/media/ethusdt_orderbook.gif)

In picture below _BinanceBot create order to Order Book only if price spread by ETH/BTC greater than 0.2%_.

__Warn:__ BinanceBot uses _test order create_  API by default (without real order creation).
Turn off `TEST_ORDER_CREATION_MODE` compilation symbol in [MarketMakerBot.cs](src/BinanceBot.Market/MarketMakerBot.cs) to _create real order_ in order book.

## Requirements

- .NET 9.0
- Binance Account.

## Setup Instructions

### Setting up API Credentials

Both console applications (`BinanceBot.MarketBot.Console` and `BinanceBot.MarketViewer.Console`) now support environment variables for API credentials using `.env` files.

1. **Copy the example file:**
   ```bash
   # For MarketBot
   cp src/BinanceBot.MarketBot.Console/.env.example src/BinanceBot.MarketBot.Console/.env
   
   # For MarketViewer
   cp src/BinanceBot.MarketViewer.Console/.env.example src/BinanceBot.MarketViewer.Console/.env
   ```

2. **Edit the `.env` file** with your actual Binance API credentials:
   ```bash
   BINANCE_API_KEY=your_actual_api_key_here
   BINANCE_SECRET=your_actual_secret_key_here
   ```

3. **Run the application** - it will automatically load the credentials from the `.env` file.

⚠️ **Never commit/share your `.env` file to version control!** It contains sensitive credentials.

## References

1. [Binance official API docs](https://github.com/binance-exchange/binance-official-api-docs).
1. [Official C# Wrapper for the Binance exchange API](https://github.com/glitch100/BinanceDotNet).
