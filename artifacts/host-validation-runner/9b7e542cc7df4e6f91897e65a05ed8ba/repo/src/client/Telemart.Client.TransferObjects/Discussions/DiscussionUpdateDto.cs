using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionUpdateDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("entity_documents")]
        public IReadOnlyCollection<DiscussionEntityDocumentDto> EntityDocuments { get; init; }
    }
}