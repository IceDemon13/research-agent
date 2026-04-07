using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record ChangeStateDto
    {
        [JsonProperty("entity_id")]
        public int EntityId { get; init; }

        [JsonProperty("document_id")]
        public int DocumentId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }
    }
}