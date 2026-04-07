using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MovementReport
{
    public sealed class MovementReportProductDto
    {
        [JsonProperty("id")]
        public int? ProductId { get; init; }

        [JsonProperty("order_id")]
        public int? OrderId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("quantity")]
        public int? Quantity { get; init; }

        [JsonProperty("in_stock")]
        public int? InStock { get; init; }

        [JsonProperty("type_id")]
        public int? TypeId { get; init; }

        [JsonProperty("barcode")]
        public string Barcode { get; init; }

        [JsonProperty("position")]
        public int? Position { get; init; }

        [JsonProperty("child_nodes")]
        public IReadOnlyCollection<MovementReportProductDto> ChildNodes { get; init; }
    }
}