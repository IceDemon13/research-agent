using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    [JsonConverter(typeof (JsonStringEnumMemberConverter))]
    public enum SellPaymentType
    {
        [EnumMember(Value = "CASH")]
        Cash,
        [EnumMember(Value = "CASHLESS")]
        Cashless,
    }
}