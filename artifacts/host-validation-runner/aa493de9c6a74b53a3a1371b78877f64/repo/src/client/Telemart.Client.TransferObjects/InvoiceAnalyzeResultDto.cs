using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceAnalyzeResultDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<InvoiceAnalyzeResultProductDto> Products { get; init; }
    }
}