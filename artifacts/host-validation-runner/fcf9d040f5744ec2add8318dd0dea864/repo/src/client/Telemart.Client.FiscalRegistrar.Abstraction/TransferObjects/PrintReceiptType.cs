using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum PrintReceiptType
    {
        [EnumMember(Value = "html")]
        Html,
        [EnumMember(Value = "pdf")]
        Pdf,
        [EnumMember(Value = "text")]
        Text
    }
}