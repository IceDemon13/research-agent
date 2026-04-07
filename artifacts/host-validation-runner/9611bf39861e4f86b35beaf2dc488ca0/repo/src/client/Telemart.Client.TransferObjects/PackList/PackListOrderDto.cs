using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public class PackListOrderDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_state_id")]
        public int OrderStateId { get; set; }

        [JsonProperty("product_count")]
        public int ProductCount { get; set; }

        [JsonProperty("row_count")]
        public int RowCount { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("simple_product_ids")]
        public IReadOnlyCollection<int> SimpleProductIds { get; init; }

        [JsonProperty("assembly_service_ids")]
        public IReadOnlyCollection<int> AssemblyServiceIds { get; init; }

        [JsonProperty("aditional_service_product_ids")]
        public IReadOnlyCollection<int> AdditionalServiceProductIds { get; init; }
    }
}