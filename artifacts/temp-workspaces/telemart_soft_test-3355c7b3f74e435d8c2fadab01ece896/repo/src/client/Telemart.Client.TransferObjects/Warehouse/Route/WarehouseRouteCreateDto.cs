using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public sealed class WarehouseRouteCreateDto
    {
        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; set; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; set; }

        [JsonProperty("weight")]
        public int? Weight { get; set; }

        [JsonProperty("route_times")]
        public WarehouseRouteTimeCreateDto[] RouteTimes { get; set; }
    }
}