using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillProductCreateDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("invoice_product_id")]
        public int InvoiceProductId { get; set; }

        [JsonProperty("tax_rate_id")]
        public int TaxRateId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("price_tax")]
        public decimal PriceTax { get; set; }

        [JsonProperty("sum")]
        public decimal Sum { get; set; }

        [JsonProperty("sum_tax")]
        public decimal SumTax { get; set; }

        [JsonProperty("tnved")]
        public long? Tnved { get; set; }
    }
}