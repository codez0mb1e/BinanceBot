using System;
using BinanceExchange.API.Enums;

namespace BinanceExchange.API.Client
{
    public class BinanceEndpointData
    {
        public readonly EndpointSecurityType SecurityType;
        public readonly Uri Uri;

        public BinanceEndpointData(Uri uri, EndpointSecurityType securityType, bool useCache = false)
        {
            Uri = uri;
            SecurityType = securityType;
            UseCache = useCache;
        }

        public bool UseCache { get; }

        public override string ToString()
        {
            return Uri.AbsoluteUri;
        }
    }
}