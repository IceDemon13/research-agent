using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public class WarehouseRouteSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("route_times")]
        public WarehouseRouteTimeDto[] RouteTimes { get; set; }
    }
}