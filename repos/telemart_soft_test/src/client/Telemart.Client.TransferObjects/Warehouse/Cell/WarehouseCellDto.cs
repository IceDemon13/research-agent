using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Cell
{
    public class WarehouseCellDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}