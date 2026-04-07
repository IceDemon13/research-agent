using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehouseConnectionDto
    {
        [JsonProperty("from_warehouse_id")]
        public int FromWarehouseId { get; set; }

        [JsonProperty("to_warehouse_id")]
        public int ToWarehouseId { get; set; }

        [JsonProperty("route_id")]
        public int RouteId { get; set; }

        [JsonProperty("route_time_id")]
        public int RouteTimeId { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }
    }
}