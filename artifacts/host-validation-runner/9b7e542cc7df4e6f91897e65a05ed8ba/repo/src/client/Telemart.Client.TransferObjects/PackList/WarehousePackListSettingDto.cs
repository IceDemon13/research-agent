using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public sealed record WarehousePackListSettingDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("product_max_lines")]
        public int? OrderProductMaxLines { get; init; }

        [JsonProperty("max_total_weight")]
        public decimal MaxTotalWeight { get; init; }

        [JsonProperty("max_sku_quantity")]
        public int MaxSkuQuantity { get; init; }
    }
}