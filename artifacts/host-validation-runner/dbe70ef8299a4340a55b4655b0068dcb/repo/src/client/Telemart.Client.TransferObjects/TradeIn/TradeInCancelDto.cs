using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCancelDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("cancel_reason_id")]
        public int CancelReasonId { get; init; }

        [JsonProperty("track_number")]
        public string TrackNumber { get; init; }
    }
}