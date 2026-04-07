using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record EntityChangeStateDto
    {
        [JsonProperty("entity_id")]
        public int EntityId { get; init; }

        [JsonProperty("from_state_id")]
        public int FromStateId { get; init; }

        [JsonProperty("to_state_id")]
        public int ToStateId { get; init; }

        [JsonProperty("info")]
        public string Info { get; init; }
    }
}