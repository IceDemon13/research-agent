using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionTypePropertyDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("discussion_type_id")]
        public int? DiscussionTypeId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("position")]
        public int Position { get; init; }

        [JsonProperty("required")]
        public bool Required { get; init; }
    }
}