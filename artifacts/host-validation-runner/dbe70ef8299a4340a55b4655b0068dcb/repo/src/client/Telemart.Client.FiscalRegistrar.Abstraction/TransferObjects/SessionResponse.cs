using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public class SessionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("state")]
        public SessionState State { get; set; }
    }
}