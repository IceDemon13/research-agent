using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceAdditionalCostProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_additional_cost_id")]
        public int InvoiceAdditionalCostId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("invoice_product_id")]
        public int InvoiceProductId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}