using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceProductSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("supplier_product_id")]
        public string SupplierProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_real")]
        public int? QuantityReal { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("preorder")]
        public bool Preorder { get; init; }
    }
}