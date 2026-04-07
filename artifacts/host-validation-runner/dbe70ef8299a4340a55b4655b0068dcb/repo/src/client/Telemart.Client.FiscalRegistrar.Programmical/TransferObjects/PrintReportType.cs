using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Programmical.TransferObjects
{
    [JsonConverter(typeof (JsonStringEnumMemberConverter))]
    public enum PrintReportType
    {
        [EnumMember(Value = "text")] Text,
        [EnumMember(Value = "xml")] Xml,
    }
}