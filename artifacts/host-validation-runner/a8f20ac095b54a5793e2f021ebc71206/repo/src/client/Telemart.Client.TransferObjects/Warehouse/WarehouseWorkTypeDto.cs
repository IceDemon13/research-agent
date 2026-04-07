using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehouseWorkTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("work")]
        public string Work { get; set; }
    }
}