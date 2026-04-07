using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("invoiced_on")]
        public DateTime InvoicedOn { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}