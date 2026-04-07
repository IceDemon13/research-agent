using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceAnalyzeResultProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("segment_name")]
        public string SegmentName { get; init; }

        [JsonProperty("result_items")]
        public IReadOnlyCollection<InvoiceAnalyzeResultItemDto> ResultItems { get; init; }
    }
}