using System.Text.Json.Serialization;
using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;

namespace Telemart.Client.FiscalRegistrar.Programmical.TransferObjects
{
    public class PrintChequeRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("receipt_id")]
        public string ReceiptId { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("type")]
        public PrintReceiptType Type { get; set; }
    }
}