using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public class CloseSessionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("z_report_id")]
        public string ZReportId { get; set; }
    }
}