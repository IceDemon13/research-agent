using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Asterisk
{
    public sealed record AsteriskStatusDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; }

        [JsonProperty("dnd")]
        public string Dnd { get; init; }

        [JsonProperty("presence")]
        public string Presence { get; init; }
    }
}