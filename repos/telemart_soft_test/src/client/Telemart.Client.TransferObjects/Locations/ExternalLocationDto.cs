using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Locations
{
    public sealed record ExternalLocationDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("type")]
        public string Type { get; init; }

        [JsonProperty("external_id")]
        public string ExternalId { get; init; }

        [JsonProperty("external_name")]
        public string ExternalName { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }
    }
}