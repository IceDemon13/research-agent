using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public sealed record WarehousePerformancePatternDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }
    }
}