using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderDocumentTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}