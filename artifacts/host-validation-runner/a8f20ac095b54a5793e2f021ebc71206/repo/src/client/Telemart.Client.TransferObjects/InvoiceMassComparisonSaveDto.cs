using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceMassComparisonSaveDto
    {
        [JsonProperty("invoices")]
        public IReadOnlyCollection<InvoiceComparisonSaveDto> Invoices { get; set; }
    }
}