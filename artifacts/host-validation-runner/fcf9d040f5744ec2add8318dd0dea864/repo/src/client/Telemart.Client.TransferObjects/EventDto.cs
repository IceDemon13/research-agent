using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects
{
    public sealed record EventDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("documents")]
        public JObject Documents { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; init; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; init; }
    }
}