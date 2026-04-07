using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Cronicle
{
    public sealed record CronicleSessionDto
    {
        [JsonProperty("session_id")]
        public string SessionId { get; init; }

        [JsonProperty("login")]
        public string Login { get; init; }
    }
}