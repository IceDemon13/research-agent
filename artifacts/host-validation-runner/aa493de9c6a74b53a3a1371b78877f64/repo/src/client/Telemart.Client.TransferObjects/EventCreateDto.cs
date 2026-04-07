using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects
{
    public sealed record EventCreateDto
    {
        public EventCreateDto(int typeId, int entityId, string comment, JObject documents)
        {
            TypeId = typeId;
            EntityId = entityId;
            Comment = comment;
            Documents = documents;
        }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("entity_id")]
        public int EntityId { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("documents")]
        public JObject Documents { get; init; }
    }
}