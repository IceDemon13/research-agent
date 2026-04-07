using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record EntityDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("display_name")]
        public string DisplayName { get; init; }

        [JsonProperty("logistic")]
        public bool Logistic { get; init; }

        [JsonProperty("discussion_view_models")]
        public string[] DiscussionViewModels { get; init; }

        [JsonProperty("db_data")]
        public EntityDbDataDto DbData { get; init; }
    }
}