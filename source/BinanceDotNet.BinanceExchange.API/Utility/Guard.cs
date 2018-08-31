using System;

namespace BinanceExchange.API.Utility
{
    internal static class Guard
    {
        public static void AgainstNullOrEmpty(string param, string name = null)
        {
            if (string.IsNullOrEmpty(param))
                throw new ArgumentNullException(name ?? "The Guarded argument was null or empty.");
        }

        public static void AgainstNull(object param, string name = null)
        {
            if (param == null)
                throw new ArgumentNullException(name ?? "The Guarded argument was null.");
        }
    }
}