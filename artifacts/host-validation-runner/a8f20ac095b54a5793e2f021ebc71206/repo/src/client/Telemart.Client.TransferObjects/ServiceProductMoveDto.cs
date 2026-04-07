using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceProductMoveDto
    {
        public ServiceProductMoveDto(int id, int warehouseId)
        {
            Id = id;
            WarehouseId = warehouseId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }
    }
}