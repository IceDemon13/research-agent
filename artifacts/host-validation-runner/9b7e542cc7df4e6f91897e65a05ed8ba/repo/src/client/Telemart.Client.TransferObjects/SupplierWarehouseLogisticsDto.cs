using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record SupplierWarehouseLogisticsDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("city_id")]
        public int CityId { get; init; }

        [JsonProperty("city_name")]
        public string CityName { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("carries")]
        public IReadOnlyCollection<SupplierCarryDto> Carries { get; init; }
    }
}