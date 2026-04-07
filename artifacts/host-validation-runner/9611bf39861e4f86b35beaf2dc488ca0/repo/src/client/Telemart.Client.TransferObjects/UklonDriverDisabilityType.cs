using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Telemart.Client.TransferObjects
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UklonDriverDisabilityType
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "deaf")]
        Deaf,
        [EnumMember(Value = "hard_hearing")]
        HardHearing
    }
}