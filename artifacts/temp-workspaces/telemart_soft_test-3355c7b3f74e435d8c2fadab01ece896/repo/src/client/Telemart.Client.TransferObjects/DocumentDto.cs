using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record DocumentDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("extension")]
        public string Extension { get; init; }
    }
}