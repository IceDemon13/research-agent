using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionEntityDocumentDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("discussion_id")]
        public int DiscussionId { get; init; }

        [JsonProperty("entity_id")]
        public int EntityId { get; init; }

        [JsonProperty("document_id")]
        public int DocumentId { get; init; }
    }
}