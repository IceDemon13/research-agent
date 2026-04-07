using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; init; }

        [JsonProperty("priority_bitrix")]
        public bool PriorityBitrix { get; init; }

        [JsonProperty("template_id")]
        public int TemplateId { get; init; }

        [JsonProperty("entity_ids")]
        public int[] EntityIds { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("template")]
        public DiscussionTemplateDto Template { get; init; }
    }
}