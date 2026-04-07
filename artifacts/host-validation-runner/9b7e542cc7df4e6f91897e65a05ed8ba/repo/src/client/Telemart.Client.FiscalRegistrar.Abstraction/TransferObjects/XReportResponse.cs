using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public class XReportResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }
    }
}