using System.Runtime.Serialization;

namespace BinanceExchange.API.Enums
{
    [DataContract]
    public enum TimeInForce
    {
        /// <summary>
        ///     Good Till Cancelled: ордер будет висеть до тех пор, пока его не отменят
        /// </summary>
        [EnumMember(Value = "GTC")] GTC = 0,

        /// <summary>
        ///     Immediate Or Cancel: будет куплено то количество, которое можно купить немедленно. Все, что не удалось купить,
        ///     будет отменено.
        /// </summary>
        [EnumMember(Value = "IOC")] IOC,

        /// <summary>
        ///     Fill-Or-Kill: либо будет куплено все указанное количество немедленно, либо не будет куплено вообще ничего, ордер
        ///     отменится.
        /// </summary>
        [EnumMember(Value = "FOK")] FOK
    }
}