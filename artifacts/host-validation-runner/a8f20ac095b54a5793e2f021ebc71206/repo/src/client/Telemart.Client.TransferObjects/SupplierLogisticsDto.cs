using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record SupplierLogisticsDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; init; }

        [JsonProperty("warehouses")]
        public IReadOnlyCollection<SupplierWarehouseLogisticsDto> Warehouses { get; init; }
    }
}