// Create test order flag. See more: https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md#test-new-order-trade 
#define TEST_ORDER_CREATION_MODE 

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Websockets;

using NLog;


namespace BinanceBot.Market
{
    /// <summary>
    /// Market Maker Bot
    /// </summary>
    public class MarketMakerBot : BaseMarketBot<NaiveMarketMakerStrategy>
    {
        private readonly IBinanceRestClient _binanceRestClient;
        private readonly IBinanceWebSocketClient _webSocketClient;
        private readonly MarketDepth _marketDepth;


        /// <param name="symbol"></param>
        /// <param name="marketStrategy"></param>
        /// <param name="webSocketClient"></param>
        /// <param name="logger"></param>
        /// <param name="binanceRestClient"></param>
        /// <exception cref="ArgumentNullException"><paramref name="symbol"/> cannot be <see langword="null"/></exception>
        /// <exception cref="ArgumentNullException"><paramref name="marketStrategy"/> cannot be <see langword="null"/></exception>
        /// <exception cref="ArgumentNullException"><paramref name="webSocketClient"/> cannot be <see langword="null"/></exception>
        /// <exception cref="ArgumentNullException"><paramref name="binanceRestClient"/> cannot be <see langword="null"/></exception>
        public MarketMakerBot(
            string symbol,
            NaiveMarketMakerStrategy marketStrategy,
            IBinanceRestClient binanceRestClient,
            IBinanceWebSocketClient webSocketClient,
            Logger logger) :
            base(symbol, marketStrategy, logger)
        {
            _marketDepth = new MarketDepth(symbol);
            _binanceRestClient = binanceRestClient ?? throw new ArgumentNullException(nameof(binanceRestClient));
            _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        }



        public override async Task ValidateConnectionAsync()
        {
            Logger.Info("Testing connection...");
            IResponse testConnectResponse = await _binanceRestClient.TestConnectivityAsync();
            if (testConnectResponse != null)
            {
                ServerTimeResponse serverTimeResponse = await _binanceRestClient.GetServerTimeAsync();
                Logger.Info($"Connection was established successfully. Approximate ping time: {DateTime.UtcNow.Subtract(serverTimeResponse.ServerTime).TotalMilliseconds:F0} ms");
            }
        }


        public override async Task<IEnumerable<OrderResponse>> GetOpenedOrdersAsync(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Invalid symbol value", nameof(symbol));

            return await _binanceRestClient.GetCurrentOpenOrdersAsync(new CurrentOpenOrdersRequest { Symbol = symbol });
        }


        public override async Task CancelOrdersAsync(IEnumerable<OrderResponse> orders)
        {
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            foreach (OrderResponse order in orders)
                await _binanceRestClient.CancelOrderAsync(new CancelOrderRequest { OrderId = order.OrderId, OriginalClientOrderId = order.ClientOrderId, Symbol = order.Symbol });
        }


        public override async Task<BaseCreateOrderResponse> CreateOrderAsync(CreateOrderRequest order)
        {

#if TEST_ORDER_CREATION_MODE
            EmptyResponse response = await _binanceRestClient.CreateTestOrderAsync(order);
            return response != null ? new AcknowledgeCreateOrderResponse() : null;
#else
            return await _binanceRestClient.CreateOrderAsync(order);
#endif
        }



        #region Run bot section
        public override async Task RunAsync()
        {
            // validate connection w/ stock
            await ValidateConnectionAsync();

            // subscribe on order book updates
            _marketDepth.MarketBestPairChanged += async (s, e) => await OnMarketBestPairChanged(s, e);


            var marketDepthManager = new MarketDepthManager(_binanceRestClient, _webSocketClient);

            // build order book
            await marketDepthManager.BuildAsync(_marketDepth);
            // stream order book updates
            marketDepthManager.StreamUpdates(_marketDepth);
        }


        private async Task OnMarketBestPairChanged(object sender, MarketBestPairChangedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            //  get current opened orders by token
            var openOrdersResponse = await GetOpenedOrdersAsync(Symbol);

            // cancel already opened orders (if necessary)
            await CancelOrdersAsync(openOrdersResponse);

            // find new market position
            Quote q = MarketStrategy.Process(e.MarketBestPair);
            // if position found then create order 
            if (q != null)
            {
                var newOrderRequest = new CreateOrderRequest
                {
                    Symbol = Symbol,
                    Quantity = q.Volume,
                    Price = q.Price,
                    Side = q.Direction,
                    Type = OrderType.Limit,
                    TimeInForce = TimeInForce.GTC // 'Good Till Cancelled' marketStrategy 
                };

                await CreateOrderAsync(newOrderRequest);
                Logger.Info($"Limit order created. Price: {newOrderRequest.Price}. Volume: {newOrderRequest.Quantity}");
            }

            Console.WriteLine(Environment.NewLine); // only for beauty console output purposes
        }
#endregion


#region Stop/dispose bot section
        public override void Stop()
        {
            Logger.Info("Bot is stopped");
            Dispose();
        }


        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _webSocketClient.Dispose();
        }
#endregion
    }
}