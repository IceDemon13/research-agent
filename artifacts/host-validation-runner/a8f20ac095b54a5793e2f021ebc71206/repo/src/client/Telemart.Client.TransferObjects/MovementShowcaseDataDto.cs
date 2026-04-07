using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class MovementShowcaseDataDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; init; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; init; }

        [JsonProperty("showcase_warehouse_id")]
        public int ShowcaseWarehouseId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }
    }
}