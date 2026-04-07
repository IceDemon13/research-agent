using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public sealed class PackListEditDto
    {
        public PackListEditDto(int collectorEmployeeId, int packagerEmployeeId, int warehouseId)
        {
            CollectorEmployeeId = collectorEmployeeId;
            PackagerEmployeeId = packagerEmployeeId;
            WarehouseId = warehouseId;
        }

        [JsonProperty("collector")]
        public int CollectorEmployeeId { get; }

        [JsonProperty("packager")]
        public int PackagerEmployeeId { get; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; }
    }
}