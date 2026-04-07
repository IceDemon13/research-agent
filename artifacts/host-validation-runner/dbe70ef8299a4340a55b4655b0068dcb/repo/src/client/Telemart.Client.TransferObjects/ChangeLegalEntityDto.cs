using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ChangeLegalEntityDto
    {
        [JsonProperty("document_id")]
        public int DocumentId { get; init; }

        [JsonProperty("entity_id")]
        public int EntityId { get; init; }

        [JsonProperty("legal_entity_id")]
        public int LegalEntityId { get; init; }
    }
}