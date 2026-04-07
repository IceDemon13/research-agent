using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceDocumentSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("return_invoice_id")]
        public int ReturnInvoiceId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}