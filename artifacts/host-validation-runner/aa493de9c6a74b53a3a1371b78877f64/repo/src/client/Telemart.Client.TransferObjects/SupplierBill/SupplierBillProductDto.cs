using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("bill_id")]
        public int BillId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("invoice_product_id")]
        public int? InvoiceProductId { get; set; }

        [JsonProperty("tax_rate_id")]
        public int TaxRateId { get; set; }

        [JsonProperty("product_name")]
        public string ProductNameRu { get; set; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

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