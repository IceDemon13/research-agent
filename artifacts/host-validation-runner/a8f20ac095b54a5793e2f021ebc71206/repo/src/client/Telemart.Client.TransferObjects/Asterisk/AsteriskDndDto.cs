using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Asterisk
{
    public sealed record AsteriskDndDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("display_text")]
        public string DisplayText { get; init; }
    }
}