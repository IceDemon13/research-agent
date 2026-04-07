using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceProductDto : ILocalіzableEntity
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("supplier_product_id")]
        public string SupplierProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("product_prefix")]
        public string ProductPrefix { get; set; }

        [JsonProperty("product_prefix_ua")]
        public string ProductPrefixUa { get; set; }

        [JsonProperty("product_prefix_ru")]
        public string ProductPrefixEn { get; set; }

        [JsonProperty("product_pn")]
        public string ProductPn { get; set; }

        [JsonProperty("product_employee_id")]
        public int ProductEmployeeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_real")]
        public int? QuantityReal { get; set; }

        [JsonProperty("quantity_returned")]
        public int QuantityReturned { get; set; }

        [JsonProperty("quantity_reserved")]
        public int QuantityReserved { get; set; }

        [JsonProperty("bills_quantity")]
        public int BillsQuantity { get; set; }

        [JsonProperty("order_quantity")]
        public int OrderQuantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("preorder")]
        public bool Preorder { get; init; }

        [JsonProperty("additional_cost")]
        public decimal AdditionalCost { get; set; }

        [JsonProperty("employee")]
        public EmployeeSimpleDto Employee { get; set; }

        [JsonProperty("serials")]
        public virtual List<string> SerialNumbers { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonIgnore]
        public string Name => ProductName;

        [JsonIgnore]
        public string NameUkr => ProductNameUa;

        [JsonIgnore]
        public string NameEn => ProductNameEn;
    }
}