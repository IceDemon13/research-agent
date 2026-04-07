using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCreateTrackNumberSourceDto
    {
        [JsonProperty("source")]
        public string Source { get; init; }

        [JsonProperty("recipient")]
        public string Recipient { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("carry_id")]
        public int CarryId { get; init; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; init; }
    }
}