using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class CreateServiceRequestTrackNumberSourceDto
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("recipient")]
        public string Recipient { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }
    }
}