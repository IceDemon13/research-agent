using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class MovementRouteScheduleDto
    {
        [JsonProperty("date_out")]
        public DateTime DateOut { get; set; }

        [JsonProperty("date_in")]
        public DateTime DateIn { get; set; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; set; }

        [JsonProperty("date_departure")]
        public DateTime DateDeparture { get; set; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; set; }

        [JsonProperty("delivery_type_id")]
        public int? DeliveryTypeId { get; set; }

        [JsonProperty("purpose_ids")]
        public IReadOnlyCollection<int> PurposeIds { get; init; }
    }
}