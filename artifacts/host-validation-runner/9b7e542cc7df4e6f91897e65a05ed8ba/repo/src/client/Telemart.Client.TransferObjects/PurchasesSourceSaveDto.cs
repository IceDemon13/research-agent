using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchasesSourceSaveDto
    {
        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("source_id")]
        public int SourceId { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }
    }
}