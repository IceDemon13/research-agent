using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record UpdateBusySecondsAfterCallDto
    {
        [JsonProperty("seconds")]
        public string Seconds { get; init; }
    }
}