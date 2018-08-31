using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Response.Error;
using Newtonsoft.Json;
using NLog;

namespace BinanceExchange.API.Client
{
    /// <summary>
    ///     The API Processor is the chief piece of functionality responsible for handling and creating requests to the API
    /// </summary>
    internal class APIProcessor : IAPIProcessor
    {
        private readonly string _apiKey;
        private readonly string _secretKey;
        private TimeSpan _cacheTime;
        private readonly Logger _logger;

        public APIProcessor(string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;

            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        ///     Set the cache expiry time
        /// </summary>
        /// <param name="time"></param>
        public void SetCacheTime(TimeSpan time)
        {
            _cacheTime = time;
        }

        /// <summary>
        ///     Processes a GET request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<T> ProcessGetRequest<T>(BinanceEndpointData endpoint, int receiveWindow = 5000)
            where T : class
        {
            var fullKey = $"{typeof(T).Name}-{endpoint.Uri.AbsoluteUri}";

            HttpResponseMessage message;
            switch (endpoint.SecurityType)
            {
                case EndpointSecurityType.ApiKey:
                case EndpointSecurityType.None:
                    message = await RequestClient.GetRequest(endpoint.Uri);
                    break;
                case EndpointSecurityType.Signed:
                    message = await RequestClient.SignedGetRequest(endpoint.Uri, _apiKey, _secretKey,
                        endpoint.Uri.Query, receiveWindow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await HandleResponse<T>(message, endpoint.ToString(), fullKey);
        }

        /// <summary>
        ///     Processes a DELETE request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<T> ProcessDeleteRequest<T>(BinanceEndpointData endpoint, int receiveWindow = 5000)
            where T : class
        {
            var fullKey = $"{typeof(T).Name}-{endpoint.Uri.AbsoluteUri}";

            HttpResponseMessage message;
            switch (endpoint.SecurityType)
            {
                case EndpointSecurityType.ApiKey:
                    message = await RequestClient.DeleteRequest(endpoint.Uri);
                    break;
                case EndpointSecurityType.Signed:
                    message = await RequestClient.SignedDeleteRequest(endpoint.Uri, _apiKey, _secretKey,
                        endpoint.Uri.Query, receiveWindow);
                    break;
                case EndpointSecurityType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await HandleResponse<T>(message, endpoint.ToString(), fullKey);
        }

        /// <summary>
        ///     Processes a POST request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<T> ProcessPostRequest<T>(BinanceEndpointData endpoint, int receiveWindow = 5000)
            where T : class
        {
            var fullKey = $"{typeof(T).Name}-{endpoint.Uri.AbsoluteUri}";

            HttpResponseMessage message;
            switch (endpoint.SecurityType)
            {
                case EndpointSecurityType.ApiKey:
                    message = await RequestClient.PostRequest(endpoint.Uri);
                    break;
                case EndpointSecurityType.None:
                    throw new ArgumentOutOfRangeException();
                case EndpointSecurityType.Signed:
                    message = await RequestClient.SignedPostRequest(endpoint.Uri, _apiKey, _secretKey,
                        endpoint.Uri.Query, receiveWindow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await HandleResponse<T>(message, endpoint.ToString(), fullKey);
        }

        /// <summary>
        ///     Processes a PUT request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="receiveWindow"></param>
        /// <returns></returns>
        public async Task<T> ProcessPutRequest<T>(BinanceEndpointData endpoint, int receiveWindow = 5000)
            where T : class
        {
            var fullKey = $"{typeof(T).Name}-{endpoint.Uri.AbsoluteUri}";

            HttpResponseMessage message;
            switch (endpoint.SecurityType)
            {
                case EndpointSecurityType.ApiKey:
                    message = await RequestClient.PutRequest(endpoint.Uri);
                    break;
                case EndpointSecurityType.None:
                case EndpointSecurityType.Signed:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await HandleResponse<T>(message, endpoint.ToString(), fullKey);
        }

        private async Task<T> HandleResponse<T>(HttpResponseMessage message, string requestMessage, string fullCacheKey)
            where T : class
        {
            if (message.IsSuccessStatusCode)
            {
                var messageJson = await message.Content.ReadAsStringAsync();
                T messageObject = null;
                try
                {
                    messageObject = JsonConvert.DeserializeObject<T>(messageJson);
                }
                catch (Exception ex)
                {
                    var deserializeErrorMessage =
                        $"Unable to deserialize message from: {requestMessage}. Exception: {ex.Message}";
                    _logger.Error(deserializeErrorMessage);
                    throw new BinanceException(deserializeErrorMessage, new BinanceError
                    {
                        RequestMessage = requestMessage,
                        Message = ex.Message
                    });
                }

                _logger.Debug($"Successful Message Response={messageJson}");

                if (messageObject == null) throw new Exception("Unable to deserialize to provided type");

                return messageObject;
            }

            var errorJson = await message.Content.ReadAsStringAsync();
            var errorObject = JsonConvert.DeserializeObject<BinanceError>(errorJson);
            if (errorObject == null) throw new BinanceException("Unexpected Error whilst handling the response", null);
            errorObject.RequestMessage = requestMessage;
            var exception = CreateBinanceException(message.StatusCode, errorObject);
            _logger.Error($"Error Message Received:{errorJson}", exception);
            throw exception;
        }

        private BinanceException CreateBinanceException(HttpStatusCode statusCode, BinanceError errorObject)
        {
            if (statusCode == HttpStatusCode.GatewayTimeout) return new BinanceTimeoutException(errorObject);
            var parsedStatusCode = (int) statusCode;
            if (parsedStatusCode >= 400 && parsedStatusCode <= 500) return new BinanceBadRequestException(errorObject);
            return parsedStatusCode >= 500
                ? new BinanceServerException(errorObject)
                : new BinanceException("Binance API Error", errorObject);
        }
    }
}