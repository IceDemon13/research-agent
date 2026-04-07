using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillCreateDto
    {
        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("invoiced_on")]
        public DateTime InvoicedOn { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("products")]
        public SupplierBillProductCreateDto[] Products { get; set; }
    }
}