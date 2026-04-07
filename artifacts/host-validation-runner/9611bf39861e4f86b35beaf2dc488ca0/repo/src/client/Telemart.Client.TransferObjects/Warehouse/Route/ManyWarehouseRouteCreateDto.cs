using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public sealed record ManyWarehouseRouteCreateDto
    {
        [JsonProperty("warehouse_routes")]
        public WarehouseRouteCreateDto[] WarehouseRoutes { get; init; }
    }
}