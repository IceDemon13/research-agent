using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record ShowcaseClusterDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("cluster_id")]
        public int ClusterId { get; init; }

        [JsonProperty("quantity_location")]
        public int QuantityLocation { get; init; }

        [JsonProperty("quantity_cluster_fact")]
        public int QuantityClusterFact { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("location_ids")]
        public int[] LocationIds { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}