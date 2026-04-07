using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceAdditionalCostCreateDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("source_id")]
        public int SourceId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("products")]
        public InvoiceAdditionalCostCreateProductDto[] Products { get; set; }
    }
}