using System;
using System.Collections.Generic;
using System.Security.Authentication;
using BinanceExchange.API.Client;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using Newtonsoft.Json;
using NLog;
using WebSocketSharp;
using Logger = NLog.Logger;

namespace BinanceExchange.API.Websockets
{
    /// <summary>
    ///     Abstract class for creating WebSocketClients
    /// </summary>
    public class AbstractBinanceWebSocketClient
    {
        private readonly IBinanceRestClient _binanceRestClient;
        private readonly Logger _logger;


        /// <summary>
        ///     Used for deletion on the fly
        /// </summary>
        protected Dictionary<Guid, BinanceWebSocket> ActiveWebSockets = new Dictionary<Guid, BinanceWebSocket>();

        protected List<BinanceWebSocket> AllSockets = new List<BinanceWebSocket>();

        /// <summary>
        ///     Base WebSocket URI for Binance API
        /// </summary>
        private readonly string BaseWebsocketUri = "wss://stream.binance.com:9443/ws";


        protected AbstractBinanceWebSocketClient(IBinanceRestClient binanceRestClient, Logger logger = null)
        {
            _binanceRestClient = binanceRestClient ?? throw new ArgumentNullException(nameof(binanceRestClient));
            _logger = logger ?? LogManager.GetCurrentClassLogger();
        }

        private SslProtocols SupportedProtocols { get; } = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;


        /// <summary>
        ///     Connect to the Depth WebSocket
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToDepthWebSocket(string symbol,
            BinanceWebSocketMessageHandler<BinanceDepthData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            _logger.Debug("Connecting to Depth Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@depth");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }


        private Guid CreateBinanceWebSocket<T>(Uri endpoint, BinanceWebSocketMessageHandler<T> messageEventHandler)
            where T : IWebSocketResponse
        {
            var websocket = new BinanceWebSocket(endpoint.AbsoluteUri);
            websocket.OnOpen += (sender, e) => { _logger.Debug($"WebSocket Opened:{endpoint.AbsoluteUri}"); };
            websocket.OnMessage += (sender, e) =>
            {
                _logger.Debug($"WebSocket Message Received on: {endpoint.AbsoluteUri}");

                var data = JsonConvert.DeserializeObject<T>(e.Data);
                messageEventHandler(data);
            };
            websocket.OnError += (sender, e) =>
            {
                _logger.Debug(e.Exception, $"WebSocket Error on {endpoint.AbsoluteUri}:");
                CloseWebSocketInstance(websocket.Id, true);
                throw new Exception("Binance WebSocket failed")
                {
                    Data =
                    {
                        {"ErrorEventArgs", e}
                    }
                };
            };

            if (!ActiveWebSockets.ContainsKey(websocket.Id)) ActiveWebSockets.Add(websocket.Id, websocket);

            AllSockets.Add(websocket);
            websocket.SslConfiguration.EnabledSslProtocols = SupportedProtocols;
            websocket.Connect();

            return websocket.Id;
        }

        /// <summary>
        ///     Close a specific WebSocket instance using the Guid provided on creation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fromError"></param>
        public void CloseWebSocketInstance(Guid id, bool fromError = false)
        {
            if (ActiveWebSockets.ContainsKey(id))
            {
                var ws = ActiveWebSockets[id];
                ActiveWebSockets.Remove(id);
                if (!fromError) ws.Close(CloseStatusCode.PolicyViolation);
            }
            else
            {
                throw new InvalidOperationException($"No Websocket exists with the Id {id.ToString()}");
            }
        }

        /// <summary>
        ///     Checks whether a specific WebSocket instance is active or not using the Guid provided on creation
        /// </summary>
        public bool IsAlive(Guid id)
        {
            if (!ActiveWebSockets.ContainsKey(id))
                throw new InvalidOperationException($"No Websocket exists with the Id {id.ToString()}");

            return ActiveWebSockets[id].IsAlive;
        }
    }
}