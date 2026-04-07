using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCreateTrackNumberResultDto
    {
        [JsonProperty("number")]
        public string Number { get; init; }

        [JsonProperty("link")]
        public string Link { get; init; }
    }
}