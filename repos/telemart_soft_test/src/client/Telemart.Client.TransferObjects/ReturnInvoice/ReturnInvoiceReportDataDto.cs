using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public sealed class ReturnInvoiceReportDataDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("invoice_received_on")]
        public DateTime InvoiceReceivedOn { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<ReturnInvoiceProductReportDataDto> Products { get; set; }
    }
}