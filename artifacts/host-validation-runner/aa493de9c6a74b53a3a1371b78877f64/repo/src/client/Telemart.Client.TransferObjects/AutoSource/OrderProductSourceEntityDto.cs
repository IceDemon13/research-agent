using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed record OrderProductSourceEntityDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}