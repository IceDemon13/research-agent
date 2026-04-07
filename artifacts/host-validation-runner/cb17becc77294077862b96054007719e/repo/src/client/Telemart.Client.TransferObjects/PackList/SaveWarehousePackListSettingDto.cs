using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public sealed class SaveWarehousePackListSettingDto
    {
        public SaveWarehousePackListSettingDto(
            int warehouseId,
            int? orderProductMaxLines,
            decimal maxTotalWeight,
            int maxSkuQuantity)
        {
            WarehouseId = warehouseId;
            OrderProductMaxLines = orderProductMaxLines;
            MaxTotalWeight = maxTotalWeight;
            MaxSkuQuantity = maxSkuQuantity;
        }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; }

        [JsonProperty("product_max_lines")]
        public int? OrderProductMaxLines { get; }

        [JsonProperty("max_total_weight")]
        public decimal MaxTotalWeight { get; }

        [JsonProperty("max_sku_quantity")]
        public int MaxSkuQuantity { get; }
    }
}