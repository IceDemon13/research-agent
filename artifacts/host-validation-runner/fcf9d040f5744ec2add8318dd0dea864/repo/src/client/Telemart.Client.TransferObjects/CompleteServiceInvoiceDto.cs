using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class CompleteServiceInvoiceDto
    {
        [JsonProperty("repair_invoices")]
        public List<RepairInvoiceDto> RepairInvoices { get; set; }
    }
}