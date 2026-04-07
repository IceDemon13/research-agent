using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CourierDeliveryDto
    {
        [JsonProperty("time_delivery_from")]
        public TimeSpan TimeDeliveryFrom { get; init; }

        [JsonProperty("time_delivery_to")]
        public TimeSpan TimeDeliveryTo { get; init; }

        [JsonProperty("days_of_week")]
        public string DaysOfWeek { get; init; }

        [JsonProperty("carry_id")]
        public int CarryId { get; init; }
    }
}