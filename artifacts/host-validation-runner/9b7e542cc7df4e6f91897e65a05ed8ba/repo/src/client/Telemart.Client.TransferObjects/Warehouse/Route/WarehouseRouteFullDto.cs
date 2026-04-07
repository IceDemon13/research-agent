using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public class WarehouseRouteFullDto : WarehouseRouteSimpleDto
    {
        [JsonProperty("route_times")]
        public WarehouseRouteTimeDto[] RouteTimes { get; set; }
    }
}