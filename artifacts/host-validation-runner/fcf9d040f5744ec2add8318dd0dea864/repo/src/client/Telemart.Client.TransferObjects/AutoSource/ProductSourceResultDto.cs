using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed record ProductSourceResultDto
    {
        [JsonProperty("Id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("parent_record_id")]
        public int? ParentRecordId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("source")]
        public ProductSourceDto Source { get; init; }

        [JsonProperty("children")]
        public IReadOnlyCollection<ProductSourceResultDto> Children { get; init; }
    }
}