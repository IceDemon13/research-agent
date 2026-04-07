using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionStateDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}