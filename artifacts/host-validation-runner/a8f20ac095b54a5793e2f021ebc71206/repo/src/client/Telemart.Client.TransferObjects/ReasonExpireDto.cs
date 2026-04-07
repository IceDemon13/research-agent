using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ReasonExpireDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("entity_id")]
        public int EntityId { get; init; }
    }
}