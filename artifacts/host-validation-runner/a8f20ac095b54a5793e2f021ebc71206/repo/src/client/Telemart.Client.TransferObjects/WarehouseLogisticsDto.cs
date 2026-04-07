using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.TransferObjects
{
    public sealed record WarehouseLogisticsDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; init; }

        [JsonProperty("warehouse_address")]
        public string WarehouseAddress { get; init; }

        [JsonProperty("warehouse_type_id")]
        public int WarehouseTypeId { get; init; }

        [JsonProperty("color_hex")]
        public string ColorHex { get; init; }

        [JsonProperty("any_deliveries")]
        public bool AnyDeliveries { get; init; }

        [JsonProperty("from_routes")]
        public IReadOnlyCollection<WarehouseRouteSimpleDto> FromRoutes { get; init; }

        [JsonProperty("to_routes")]
        public IReadOnlyCollection<WarehouseRouteSimpleDto> ToRoutes { get; init; }

        [JsonProperty("service_center_routes")]
        public IReadOnlyCollection<WarehouseServiceCenterRouteDto> ServiceCenterRoutes { get; init; }
    }
}