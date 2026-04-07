using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceAdditionalCostCreateProductDto
    {
        [JsonProperty("invoice_product_id")]
        public int InvoiceProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}