using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Programmical.TransferObjects
{
    public class PrintReportRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("report_id")]
        public string ReportId { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("type")]
        public PrintReportType Type { get; set; }
    }
}