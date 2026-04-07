using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Programmical.TransferObjects
{
    public class CashCollectionRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}