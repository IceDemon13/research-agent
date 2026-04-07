using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class SourceWarehouseKindDto
    {
        public SourceWarehouseKindDto(int warehouseKindId, int position)
        {
            WarehouseKindId = warehouseKindId;
            Position = position;
        }

        public SourceWarehouseKindDto()
        {
        }

        [JsonProperty("warehouse_kind_id")]
        public int WarehouseKindId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}