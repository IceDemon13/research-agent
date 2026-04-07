using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateWarehouseNpWarehouseDto
    {
        public UpdateWarehouseNpWarehouseDto(int warehouseId, string npWarehouseRef)
        {
            WarehouseId = warehouseId;
            NpWarehouseRef = npWarehouseRef;
        }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("np_warehouse_ref")]
        public string NpWarehouseRef { get; set; }
    }
}