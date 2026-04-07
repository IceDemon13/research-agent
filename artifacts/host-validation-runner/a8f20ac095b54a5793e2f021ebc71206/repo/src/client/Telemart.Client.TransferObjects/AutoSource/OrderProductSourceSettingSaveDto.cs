using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class OrderProductSourceSettingSaveDto
    {
        public OrderProductSourceSettingSaveDto(int sourceId, int productTypeId, int? carryId, int? warehouseTypeId, int priority)
        {
            SourceId = sourceId;
            ProductTypeId = productTypeId;
            CarryId = carryId;
            WarehouseTypeId = warehouseTypeId;
            Priority = priority;
        }

        [JsonProperty("source_id")]
        public int SourceId { get; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; }

        [JsonProperty("warehouse_type_id")]
        public int? WarehouseTypeId { get; }

        [JsonProperty("priority")]
        public int Priority { get; }
    }
}