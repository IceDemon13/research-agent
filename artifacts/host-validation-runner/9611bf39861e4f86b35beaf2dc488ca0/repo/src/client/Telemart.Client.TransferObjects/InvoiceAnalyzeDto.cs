using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceAnalyzeDto
    {
        public InvoiceAnalyzeDto(int invoiceId, IReadOnlyCollection<InvoiceAnalyzeProductDto> products)
        {
            InvoiceId = invoiceId;
            Products = products;
        }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<InvoiceAnalyzeProductDto> Products { get; init; }
    }
}