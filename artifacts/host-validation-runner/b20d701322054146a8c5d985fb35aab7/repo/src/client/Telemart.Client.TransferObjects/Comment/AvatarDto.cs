using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Comment
{
    public sealed record AvatarDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}