using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record LogisticsMapDto
    {
        [JsonProperty("contractor_logistics")]
        public IReadOnlyCollection<SupplierLogisticsDto> SupplierLogistics { get; init; }

        [JsonProperty("warehouse_logistics")]
        public IReadOnlyCollection<WarehouseLogisticsDto> WarehouseLogistics { get; init; }

        [JsonProperty("service_centers")]
        public IReadOnlyCollection<ServiceCenterDto> ServiceCenters { get; init; }
    }
}