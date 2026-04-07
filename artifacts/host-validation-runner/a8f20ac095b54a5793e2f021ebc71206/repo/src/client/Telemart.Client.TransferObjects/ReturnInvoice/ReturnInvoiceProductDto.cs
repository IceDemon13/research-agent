using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("out_quantity")]
        public int OutQuantity { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_numbers")]
        public List<string> SerialNumbers { get; set; }

        [JsonProperty("product_pn")]
        public string ProductPn { get; set; }

        [JsonProperty("accept_quantity")]
        public int? AcceptQuantity { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("bills_quantity")]
        public int? BillsQuantity { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("full_name_ukr")]
        public string FullNameUkr { get; set; }

        [JsonProperty("category_type_id")]
        public int CategoryTypeId { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("usd_currency")]
        public int UsdCurrency { get; set; }

        [JsonProperty("max_quantity")]
        public int MaxQuantityDefaultFromInvoice { get; set; }
    }
}