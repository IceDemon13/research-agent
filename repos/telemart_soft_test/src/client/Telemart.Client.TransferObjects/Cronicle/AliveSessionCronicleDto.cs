using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Cronicle
{
    public sealed record AliveSessionCronicleDto
    {
        [JsonProperty("is_alive")]
        public bool IsAlive { get; init; }
    }
}