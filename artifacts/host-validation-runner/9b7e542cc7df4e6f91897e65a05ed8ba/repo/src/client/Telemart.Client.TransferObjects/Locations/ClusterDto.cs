using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Locations
{
    public sealed class ClusterDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("location_ids")]
        public int[] LocationIds { get; init; }
    }
}