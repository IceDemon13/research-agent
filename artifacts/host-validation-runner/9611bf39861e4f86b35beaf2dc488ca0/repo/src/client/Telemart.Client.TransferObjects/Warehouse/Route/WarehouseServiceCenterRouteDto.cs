using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public sealed record WarehouseServiceCenterRouteDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; init; }

        [JsonProperty("service_center_id")]
        public int ServiceCenterId { get; init; }
    }
}