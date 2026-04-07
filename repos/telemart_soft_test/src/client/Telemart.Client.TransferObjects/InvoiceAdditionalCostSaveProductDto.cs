using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceAdditionalCostSaveProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_product_id")]
        public int InvoiceProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}