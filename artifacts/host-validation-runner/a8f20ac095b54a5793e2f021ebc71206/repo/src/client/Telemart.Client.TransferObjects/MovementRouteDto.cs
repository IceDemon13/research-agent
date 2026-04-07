using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class MovementRouteDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; set; }

        [JsonProperty("warehouse_from_name")]
        public string WarehouseFromName { get; set; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; set; }

        [JsonProperty("warehouse_to_name")]
        public string WarehouseToName { get; set; }

        [JsonProperty("schedules")]
        public List<MovementRouteScheduleDto> Schedules { get; set; }
    }
}