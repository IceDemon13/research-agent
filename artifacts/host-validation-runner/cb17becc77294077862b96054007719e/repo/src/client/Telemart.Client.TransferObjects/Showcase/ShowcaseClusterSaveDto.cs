using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record ShowcaseClusterSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("cluster_id")]
        public int ClusterId { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("quantity_location")]
        public int QuantityLocation { get; init; }
    }
}