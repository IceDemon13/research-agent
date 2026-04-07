using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum SessionState
    {
        [EnumMember(Value = "CREATED")]
        Created,
        [EnumMember(Value = "OPENED")]
        Opened,
        [EnumMember(Value = "CLOSED")]
        Closed
    }
}