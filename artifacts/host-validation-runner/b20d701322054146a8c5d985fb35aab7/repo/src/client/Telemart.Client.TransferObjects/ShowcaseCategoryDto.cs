using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ShowcaseCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("place_name")]
        public string PlaceName { get; init; }

        [JsonProperty("cluster_id")]
        public int? ClusterId { get; init; }

        [JsonProperty("location_id")]
        public int? LocationId { get; init; }

        [JsonProperty("quantity_location")]
        public int QuantityLocation { get; init; }
    }
}